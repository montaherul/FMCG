using System.Security.Cryptography;
using TobaccoSaaS.Application.Common.Exceptions;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Domain.Entities.Audit;
using TobaccoSaaS.Domain.Entities.Identity;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Application.Features.Rbac;

public sealed class RbacService : IRbacService
{
    private readonly IUnitOfWork _uow;
    private readonly IIdentityRepository _identity;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditWriter _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public RbacService(
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

    public async Task<List<RoleDto>> ListRolesAsync(CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var roles = await _uow.Repository<Role>().ListAsync(r => r.TenantId == tenantId, ct);
        var links = await _uow.Repository<RolePermission>().ListAsync(
            rp => roles.Select(r => r.Id).Contains(rp.RoleId), ct);

        return roles
            .Select(r => new RoleDto(r.Id, r.Code, r.Name, r.Scope, r.IsSystem,
                links.Count(l => l.RoleId == r.Id)))
            .OrderBy(r => r.Name)
            .ToList();
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _uow.Repository<Role>().AnyAsync(r => r.TenantId == tenantId && r.Code == code, ct))
        {
            throw new ConflictException($"A role with code '{code}' already exists.");
        }

        var permissions = await _uow.Repository<Permission>().ListAsync(null, ct);
        var role = new Role
        {
            TenantId = tenantId,
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description,
            Scope = RoleScope.Tenant,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };
        await _uow.Repository<Role>().AddAsync(role, ct);
        await ReplacePermissionsAsync(role, permissions, request.PermissionCodes, ct);
        await _uow.SaveChangesAsync(ct);

        await WriteAuditAsync("role.create", role.Id.ToString(), $"Created role '{code}'.", ct);
        return new RoleDto(role.Id, role.Code, role.Name, role.Scope, role.IsSystem, request.PermissionCodes.Count);
    }

    public async Task<RoleDto> UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var role = await _uow.Repository<Role>().GetByIdAsync(roleId, ct)
            ?? throw new NotFoundException("Role not found.");

        if (role.TenantId != tenantId || role.Scope == RoleScope.Platform)
        {
            throw new ForbiddenException("You cannot modify this role.");
        }

        var permissions = await _uow.Repository<Permission>().ListAsync(null, ct);
        var existing = await _uow.Repository<RolePermission>().ListAsync(rp => rp.RoleId == roleId, ct);
        foreach (var link in existing)
        {
            _uow.Repository<RolePermission>().Remove(link);
        }

        await ReplacePermissionsAsync(role, permissions, request.PermissionCodes, ct);
        await _uow.SaveChangesAsync(ct);

        await WriteAuditAsync("role.permissions.update", role.Id.ToString(),
            $"Updated permissions for role '{role.Code}'.", ct);
        return new RoleDto(role.Id, role.Code, role.Name, role.Scope, role.IsSystem, request.PermissionCodes.Count);
    }

    public async Task<List<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default)
    {
        var permissions = await _uow.Repository<Permission>().ListAsync(null, ct);
        return permissions
            .Where(p => p.Module != "platform" || _currentUser.IsPlatformScope)
            .Select(p => new PermissionDto(p.Module, p.Code, p.Description))
            .OrderBy(p => p.Module).ThenBy(p => p.Code)
            .ToList();
    }

    public async Task<List<UserSummaryDto>> ListUsersAsync(CancellationToken ct = default)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new ForbiddenException("User listing is only available within a tenant.");

        var users = await _identity.ListUsersAsync(tenantId, ct);
        var result = new List<UserSummaryDto>();
        foreach (var user in users)
        {
            var roles = await _identity.GetRoleCodesAsync(user.Id, ct);
            var scope = await _uow.Repository<UserScope>().ListAsync(s => s.UserId == user.Id && s.IsActive, ct);
            result.Add(new UserSummaryDto(user.Id, user.Email, user.FullName, user.Status, roles,
                scope.FirstOrDefault()?.ScopeNodeId));
        }

        return result;
    }

    public async Task<CreatedUserResult> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new ForbiddenException("Users can only be created within a tenant.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _identity.EmailExistsAsync(email, ct))
        {
            throw new ConflictException($"A user with email '{email}' already exists.");
        }

        string? temporaryPassword = null;
        var password = request.Password;
        if (string.IsNullOrWhiteSpace(password))
        {
            temporaryPassword = GenerateTemporaryPassword();
            password = temporaryPassword;
        }

        var user = new User
        {
            TenantId = tenantId,
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = _passwordHasher.Hash(password!),
            Status = UserStatus.Active,
            MustChangePassword = temporaryPassword is not null,
            PasswordChangedAt = _clock.UtcNow,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };
        await _uow.Repository<User>().AddAsync(user, ct);
        await _uow.Repository<UserScope>().AddAsync(new UserScope { UserId = user.Id, ScopeLevel = "TENANT", IsActive = true }, ct);

        foreach (var roleId in request.RoleIds ?? Array.Empty<Guid>())
        {
            await _uow.Repository<UserRole>().AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                AssignedBy = _currentUser.UserId
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        await WriteAuditAsync("user.create", user.Id.ToString(), $"Created user '{email}'.", ct);
        return new CreatedUserResult(user.Id, email, temporaryPassword);
    }

    public async Task AssignRoleAsync(AssignRoleRequest request, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var user = await _uow.Repository<User>().GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User not found.");
        var role = await _uow.Repository<Role>().GetByIdAsync(request.RoleId, ct)
            ?? throw new NotFoundException("Role not found.");

        if (user.TenantId != tenantId || role.TenantId != tenantId)
        {
            throw new ForbiddenException("User and role must belong to the current tenant.");
        }

        if (await _uow.Repository<UserRole>().AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, ct))
        {
            return;
        }

        await _uow.Repository<UserRole>().AddAsync(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            AssignedBy = _currentUser.UserId
        }, ct);
        await _uow.SaveChangesAsync(ct);

        await WriteAuditAsync("role.assign", user.Id.ToString(),
            $"Assigned role '{role.Code}' to {user.Email}.", ct);
    }

    public async Task SetUserScopeAsync(SetUserScopeRequest request, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var user = await _uow.Repository<User>().GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        if (user.TenantId != tenantId)
        {
            throw new ForbiddenException("User must belong to the current tenant.");
        }

        var scopes = await _uow.Repository<UserScope>().ListAsync(s => s.UserId == user.Id && s.IsActive, ct);
        foreach (var scope in scopes)
        {
            scope.IsActive = false;
            scope.UpdatedBy = _currentUser.UserId;
            _uow.Repository<UserScope>().Update(scope);
        }

        await _uow.Repository<UserScope>().AddAsync(new UserScope
        {
            UserId = user.Id,
            ScopeNodeId = request.ScopeNodeId,
            ScopeLevel = request.ScopeLevel,
            IsActive = true,
            EffectiveFrom = _clock.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);
        await WriteAuditAsync("user.scope.change", user.Id.ToString(),
            $"Scope set to {request.ScopeLevel} ({request.ScopeNodeId}).", ct);
    }

    private async Task ReplacePermissionsAsync(Role role, List<Permission> permissions, IReadOnlyList<string> codes, CancellationToken ct)
    {
        foreach (var code in codes.Distinct())
        {
            var permission = permissions.FirstOrDefault(p => p.Code == code);
            if (permission is null)
            {
                continue;
            }

            await _uow.Repository<RolePermission>().AddAsync(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            }, ct);
        }
    }

    private Guid? ResolveTenantId()
    {
        if (_currentUser.IsPlatformScope)
        {
            return null;
        }

        return _currentUser.TenantId
            ?? throw new ForbiddenException("A tenant context is required.");
    }

    private Task WriteAuditAsync(string action, string entityId, string detail, CancellationToken ct)
    {
        return _audit.WriteAsync(new AuditLog
        {
            TenantId = _currentUser.TenantId,
            UserId = _currentUser.UserId,
            Action = action,
            Module = "Identity",
            EntityType = "Role",
            EntityId = entityId,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { detail }),
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent
        }, ct);
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
}
