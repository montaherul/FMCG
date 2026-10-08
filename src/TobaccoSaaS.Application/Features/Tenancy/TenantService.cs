using System.Security.Cryptography;
using TobaccoSaaS.Application.Common.Exceptions;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Application.Common.Models;
using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Entities.Audit;
using TobaccoSaaS.Domain.Entities.Identity;
using TobaccoSaaS.Domain.Entities.Tenancy;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Application.Features.Tenancy;

public sealed class TenantService : ITenantService
{
    private readonly IUnitOfWork _uow;
    private readonly IIdentityRepository _identity;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditWriter _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public TenantService(
        IUnitOfWork uow,
        IIdentityRepository identity,
        IPasswordHasher passwordHasher,
        IAuditWriter audit,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _uow = uow;
        _identity = identity;
        _passwordHasher = passwordHasher;
        _audit = audit;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PagedResult<TenantDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        EnsurePlatform();
        var (p, s) = Normalize(page, pageSize);
        var result = await _uow.Repository<Tenant>().PagedAsync(null, p, s, ct);

        var planCodes = await PlanCodesAsync(ct);
        return new PagedResult<TenantDto>(
            result.Items.Select(t => ToDto(t, planCodes.GetValueOrDefault(t.Id))).ToList(),
            result.Page, result.PageSize, result.Total);
    }

    public async Task<TenantDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _uow.Repository<Tenant>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Tenant not found.");

        if (!_currentUser.IsPlatformScope && _currentUser.TenantId != id)
        {
            throw new NotFoundException("Tenant not found.");
        }

        var planCodes = await PlanCodesAsync(ct);
        return ToDto(tenant, planCodes.GetValueOrDefault(tenant.Id));
    }

    public async Task<ProvisionTenantResult> ProvisionAsync(CreateTenantRequest request, CancellationToken ct = default)
    {
        EnsurePlatform();
        var slug = request.Slug.Trim().ToLowerInvariant();
        var email = request.AdminEmail.Trim().ToLowerInvariant();

        if (await _uow.Repository<Tenant>().AnyAsync(t => t.Slug == slug, ct))
        {
            throw new ConflictException($"A tenant with slug '{slug}' already exists.");
        }

        if (await _identity.EmailExistsAsync(email, ct))
        {
            throw new ConflictException($"A user with email '{email}' already exists.");
        }

        var plan = await ResolvePlanAsync(request.PlanCode, ct);
        var now = _clock.UtcNow;
        var temporaryPassword = GenerateTemporaryPassword();

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Status = TenantStatus.Trial,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };
        await _uow.Repository<Tenant>().AddAsync(tenant, ct);

        await _uow.Repository<Subscription>().AddAsync(new Subscription
        {
            TenantId = tenant.Id,
            PlanId = plan.Id,
            Status = SubscriptionStatus.Trialing,
            StartedAt = now,
            TrialEndsAt = now.AddDays(30)
        }, ct);

        await _uow.Repository<TenantSetting>().AddAsync(new TenantSetting { TenantId = tenant.Id, Key = "fiscal.year.start.month", ValueJson = "7", Group = "Fiscal" }, ct);
        await _uow.Repository<TenantSetting>().AddAsync(new TenantSetting { TenantId = tenant.Id, Key = "visit.radius.meters", ValueJson = "150", Group = "Field" }, ct);
        await _uow.Repository<TenantSetting>().AddAsync(new TenantSetting { TenantId = tenant.Id, Key = "timezone", ValueJson = "Asia/Dhaka", Group = "General" }, ct);
        await _uow.Repository<TenantSetting>().AddAsync(new TenantSetting { TenantId = tenant.Id, Key = "currency", ValueJson = "BDT", Group = "General" }, ct);

        var permissions = await _uow.Repository<Permission>().ListAsync(null, ct);
        Role? adminRole = null;

        foreach (var template in RoleTemplates.All.Where(r => r.Scope == RoleScope.Tenant))
        {
            var role = new Role
            {
                TenantId = tenant.Id,
                Code = template.Code,
                Name = template.Name,
                Scope = RoleScope.Tenant,
                IsSystem = true,
                CreatedBy = _currentUser.UserId,
                UpdatedBy = _currentUser.UserId
            };
            await _uow.Repository<Role>().AddAsync(role, ct);

            foreach (var code in template.Permissions)
            {
                var permission = permissions.FirstOrDefault(p => p.Code == code);
                if (permission is not null)
                {
                    await _uow.Repository<RolePermission>().AddAsync(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    }, ct);
                }
            }

            if (template.Code == "TENANT_ADMIN")
            {
                adminRole = role;
            }
        }

        var admin = new User
        {
            TenantId = tenant.Id,
            Email = email,
            FullName = request.AdminFullName.Trim(),
            PasswordHash = _passwordHasher.Hash(temporaryPassword),
            Status = UserStatus.Active,
            MustChangePassword = true,
            PasswordChangedAt = now,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };
        await _uow.Repository<User>().AddAsync(admin, ct);

        await _uow.Repository<UserRole>().AddAsync(new UserRole
        {
            UserId = admin.Id,
            RoleId = adminRole!.Id,
            AssignedBy = _currentUser.UserId
        }, ct);

        await _uow.Repository<UserScope>().AddAsync(new UserScope
        {
            UserId = admin.Id,
            ScopeLevel = "TENANT",
            IsActive = true
        }, ct);

        await _uow.SaveChangesAsync(ct);
        await WriteAuditAsync(tenant.Id, "tenant.provision", tenant.Id.ToString(),
            $"Provisioned tenant '{tenant.Name}' with admin {email}.", ct);

        return new ProvisionTenantResult(ToDto(tenant, plan.Code), email, temporaryPassword);
    }

    public async Task<TenantDto> UpdateStatusAsync(Guid id, UpdateTenantStatusRequest request, CancellationToken ct = default)
    {
        EnsurePlatform();
        var tenant = await _uow.Repository<Tenant>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Tenant not found.");

        var old = tenant.Status;
        tenant.Status = request.Status;
        tenant.UpdatedBy = _currentUser.UserId;
        _uow.Repository<Tenant>().Update(tenant);
        await _uow.SaveChangesAsync(ct);

        await WriteAuditAsync(tenant.Id, "tenant.status.change", tenant.Id.ToString(),
            $"Tenant status changed from {old} to {tenant.Status}.", ct);

        var planCodes = await PlanCodesAsync(ct);
        return ToDto(tenant, planCodes.GetValueOrDefault(tenant.Id));
    }

    public async Task<List<TenantSettingDto>> GetSettingsAsync(Guid tenantId, CancellationToken ct = default)
    {
        EnsureTenantAccess(tenantId);
        var settings = await _uow.Repository<TenantSetting>()
            .ListAsync(s => s.TenantId == tenantId, ct);
        return settings.Select(s => new TenantSettingDto(s.Key, s.ValueJson, s.Group)).ToList();
    }

    public async Task UpsertSettingAsync(Guid tenantId, UpsertSettingRequest request, CancellationToken ct = default)
    {
        EnsureTenantAccess(tenantId);
        var key = request.Key.Trim();
        var existing = (await _uow.Repository<TenantSetting>()
            .ListAsync(s => s.TenantId == tenantId && s.Key == key, ct)).FirstOrDefault();

        if (existing is null)
        {
            await _uow.Repository<TenantSetting>().AddAsync(new TenantSetting
            {
                TenantId = tenantId,
                Key = key,
                ValueJson = request.ValueJson,
                Group = request.Group
            }, ct);
        }
        else
        {
            existing.ValueJson = request.ValueJson;
            existing.Group = request.Group;
            existing.UpdatedBy = _currentUser.UserId;
            _uow.Repository<TenantSetting>().Update(existing);
        }

        await _uow.SaveChangesAsync(ct);
        await WriteAuditAsync(tenantId, "tenant.setting.upsert", key, $"Setting '{key}' updated.", ct);
    }

    public async Task<List<TenantFeatureDto>> GetFeaturesAsync(Guid tenantId, CancellationToken ct = default)
    {
        EnsureTenantAccess(tenantId);
        var flags = await _uow.Repository<FeatureFlag>().ListAsync(null, ct);
        var overrides = await _uow.Repository<TenantFeature>()
            .ListAsync(f => f.TenantId == tenantId, ct);

        return flags.Select(f =>
        {
            var overrideRow = overrides.FirstOrDefault(o => o.FeatureFlagId == f.Id);
            var enabled = overrideRow?.Enabled ?? f.DefaultState == FeatureFlagState.Enabled;
            return new TenantFeatureDto(f.Id, f.Code, f.Name, enabled, f.DefaultState);
        }).ToList();
    }

    public async Task SetFeatureAsync(Guid tenantId, SetFeatureRequest request, CancellationToken ct = default)
    {
        EnsureTenantAccess(tenantId);
        var flag = (await _uow.Repository<FeatureFlag>().ListAsync(f => f.Code == request.Code, ct)).FirstOrDefault()
            ?? throw new NotFoundException($"Feature flag '{request.Code}' not found.");

        var existing = (await _uow.Repository<TenantFeature>()
            .ListAsync(f => f.TenantId == tenantId && f.FeatureFlagId == flag.Id, ct)).FirstOrDefault();

        if (existing is null)
        {
            await _uow.Repository<TenantFeature>().AddAsync(new TenantFeature
            {
                TenantId = tenantId,
                FeatureFlagId = flag.Id,
                Enabled = request.Enabled,
                EnabledAt = request.Enabled ? _clock.UtcNow : null
            }, ct);
        }
        else
        {
            existing.Enabled = request.Enabled;
            existing.EnabledAt = request.Enabled ? _clock.UtcNow : null;
            existing.UpdatedBy = _currentUser.UserId;
            _uow.Repository<TenantFeature>().Update(existing);
        }

        await _uow.SaveChangesAsync(ct);
        await WriteAuditAsync(tenantId, "tenant.feature.set", flag.Code,
            $"Feature '{flag.Code}' set to {request.Enabled}.", ct);
    }

    private async Task<Dictionary<Guid, string>> PlanCodesAsync(CancellationToken ct)
    {
        var plans = await _uow.Repository<Plan>().ListAsync(null, ct);
        var subscriptions = await _uow.Repository<Subscription>().ListAsync(null, ct);
        var map = new Dictionary<Guid, string>();
        foreach (var subscription in subscriptions)
        {
            var plan = plans.FirstOrDefault(p => p.Id == subscription.PlanId);
            if (plan is not null)
            {
                map[subscription.TenantId] = plan.Code;
            }
        }

        return map;
    }

    private async Task<Plan> ResolvePlanAsync(string planCode, CancellationToken ct)
    {
        var plans = await _uow.Repository<Plan>().ListAsync(p => p.IsActive, ct);
        return plans.FirstOrDefault(p => string.Equals(p.Code, planCode, StringComparison.OrdinalIgnoreCase))
            ?? plans.FirstOrDefault()
            ?? throw new BusinessRuleException("No active subscription plan is configured.");
    }

    private void EnsurePlatform()
    {
        if (!_currentUser.IsPlatformScope)
        {
            throw new ForbiddenException("This action requires platform scope.");
        }
    }

    private void EnsureTenantAccess(Guid tenantId)
    {
        if (!_currentUser.IsPlatformScope && _currentUser.TenantId != tenantId)
        {
            throw new ForbiddenException("You cannot access another tenant's data.");
        }
    }

    private static TenantDto ToDto(Tenant tenant, string? planCode) =>
        new(tenant.Id, tenant.Name, tenant.Slug, tenant.Status, tenant.Country, tenant.Timezone, planCode, tenant.CreatedAt);

    private static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;
        return (page, pageSize);
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%";
        const string all = upper + lower + digits + special;

        Span<char> buffer = stackalloc char[12];
        var bytes = RandomNumberGenerator.GetBytes(buffer.Length);
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = all[bytes[i] % all.Length];
        }

        buffer[0] = upper[bytes[0] % upper.Length];
        buffer[1] = lower[bytes[1] % lower.Length];
        buffer[2] = digits[bytes[2] % digits.Length];
        buffer[3] = special[bytes[3] % special.Length];
        return new string(buffer);
    }

    private Task WriteAuditAsync(Guid tenantId, string action, string entityId, string detail, CancellationToken ct)
    {
        return _audit.WriteAsync(new AuditLog
        {
            TenantId = tenantId,
            UserId = _currentUser.UserId,
            Action = action,
            Module = "Tenancy",
            EntityType = "Tenant",
            EntityId = entityId,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { detail }),
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent
        }, ct);
    }
}
