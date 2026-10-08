using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Entities.Identity;
using TobaccoSaaS.Domain.Entities.Tenancy;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Infrastructure.Data.Seed;

/// <summary>
/// Idempotent platform seed (spec Appendix C): permission catalog, feature flags, plans,
/// platform roles and one platform admin. Safe to run on every startup.
/// </summary>
public sealed class DataSeeder : IDataSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedFeatureFlagsAsync(ct);
        await SeedPlansAsync(ct);
        await SeedPlatformRolesAsync(ct);
        await SeedPlatformAdminAsync(ct);
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var existing = await _context.Permissions.IgnoreQueryFilters().Select(p => p.Code).ToListAsync(ct);
        var toAdd = PermissionCatalog.All
            .Where(p => !existing.Contains(p.Code))
            .Select(p => new Permission { Module = p.Module, Code = p.Code })
            .ToList();

        if (toAdd.Count > 0)
        {
            await _context.Permissions.AddRangeAsync(toAdd, ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} permissions.", toAdd.Count);
        }
    }

    private async Task SeedFeatureFlagsAsync(CancellationToken ct)
    {
        var definitions = new (string Code, string Name, string Category, FeatureFlagState State)[]
        {
            ("MODULE_EMPLOYEE", "Employee & User Management", "Core", FeatureFlagState.Enabled),
            ("MODULE_ORG", "Organization & Geography", "Core", FeatureFlagState.Enabled),
            ("MODULE_DISTRIBUTOR", "Distributor Management", "Core", FeatureFlagState.Enabled),
            ("MODULE_OUTLET", "Outlet Management", "Core", FeatureFlagState.Enabled),
            ("MODULE_PRODUCT", "Product & Pricing", "Core", FeatureFlagState.Enabled),
            ("MODULE_ROUTE", "Route Management", "Field", FeatureFlagState.Enabled),
            ("MODULE_ATTENDANCE", "Attendance & GPS", "Field", FeatureFlagState.Enabled),
            ("MODULE_VISIT", "Visit Management", "Field", FeatureFlagState.Enabled),
            ("MODULE_SALES_ORDER", "Sales Orders", "Sales", FeatureFlagState.Enabled),
            ("MODULE_TARGET", "Targets & Achievements", "Sales", FeatureFlagState.Enabled),
            ("MODULE_CAMPAIGN", "Marketing Campaigns", "Marketing", FeatureFlagState.Disabled),
            ("MODULE_TRADE", "Trade Marketing", "Marketing", FeatureFlagState.Disabled),
            ("MODULE_COMPETITOR", "Competitor Intelligence", "Marketing", FeatureFlagState.Disabled),
            ("MODULE_POSM", "POSM Management", "Marketing", FeatureFlagState.Disabled),
            ("MODULE_STOCK", "Stock & Inventory", "Distribution", FeatureFlagState.Enabled),
            ("MODULE_INVOICE", "Invoicing", "Distribution", FeatureFlagState.Enabled),
            ("MODULE_PAYMENT", "Payments", "Distribution", FeatureFlagState.Enabled),
            ("MODULE_REPORTS", "Reports & Analytics", "Platform", FeatureFlagState.Enabled),
            ("MODULE_AUDIT", "Audit Log", "Platform", FeatureFlagState.Enabled),
            ("MODULE_API", "Public API", "Platform", FeatureFlagState.Preview)
        };

        var existing = await _context.FeatureFlags.IgnoreQueryFilters().Select(f => f.Code).ToListAsync(ct);
        var toAdd = definitions
            .Where(d => !existing.Contains(d.Code))
            .Select(d => new FeatureFlag { Code = d.Code, Name = d.Name, Category = d.Category, DefaultState = d.State, ModuleKey = d.Code })
            .ToList();

        if (toAdd.Count > 0)
        {
            await _context.FeatureFlags.AddRangeAsync(toAdd, ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} feature flags.", toAdd.Count);
        }
    }

    private async Task SeedPlansAsync(CancellationToken ct)
    {
        var definitions = new (string Code, string Name, long Price, int Users, int Outlets, long Tx, int Storage, int Sort)[]
        {
            ("FREE", "Free", 0, 5, 100, 1_000, 500, 1),
            ("STARTER", "Starter", 250_000, 25, 2_000, 50_000, 5_000, 2),
            ("BUSINESS", "Business", 750_000, 100, 10_000, 500_000, 25_000, 3),
            ("ENTERPRISE", "Enterprise", 2_500_000, 1_000, 100_000, 5_000_000, 200_000, 4)
        };

        var existing = await _context.Plans.IgnoreQueryFilters().Select(p => p.Code).ToListAsync(ct);
        var toAdd = definitions
            .Where(d => !existing.Contains(d.Code))
            .Select(d => new Plan
            {
                Code = d.Code,
                Name = d.Name,
                PriceMonthlyMinor = d.Price,
                MaxUsers = d.Users,
                MaxOutlets = d.Outlets,
                MaxTransactionsPerYear = d.Tx,
                StorageMb = d.Storage,
                SortOrder = d.Sort
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            await _context.Plans.AddRangeAsync(toAdd, ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} plans.", toAdd.Count);
        }
    }

    private async Task SeedPlatformRolesAsync(CancellationToken ct)
    {
        var permissionMap = await _context.Permissions.IgnoreQueryFilters()
            .ToDictionaryAsync(p => p.Code, p => p.Id, ct);

        var platformTemplates = RoleTemplates.All.Where(r => r.Scope == RoleScope.Platform).ToList();
        var existingCodes = await _context.Roles.IgnoreQueryFilters()
            .Where(r => r.TenantId == null)
            .Select(r => r.Code)
            .ToListAsync(ct);

        foreach (var template in platformTemplates.Where(t => !existingCodes.Contains(t.Code)))
        {
            var role = new Role
            {
                TenantId = null,
                Code = template.Code,
                Name = template.Name,
                Scope = RoleScope.Platform,
                IsSystem = true
            };
            await _context.Roles.AddAsync(role, ct);

            foreach (var code in template.Permissions.Where(permissionMap.ContainsKey))
            {
                await _context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionMap[code]
                }, ct);
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task SeedPlatformAdminAsync(CancellationToken ct)
    {
        var email = (_configuration["Seed:PlatformAdminEmail"] ?? "platform.admin@tobaccosaas.local").Trim().ToLowerInvariant();
        var password = _configuration["Seed:PlatformAdminPassword"] ?? "ChangeMe!12345";
        var fullName = _configuration["Seed:PlatformAdminName"] ?? "Platform Administrator";

        var exists = await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, ct);
        if (exists)
        {
            return;
        }

        var role = await _context.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TenantId == null && r.Code == "PLATFORM_ADMIN", ct)
            ?? await _context.Roles.IgnoreQueryFilters().FirstAsync(r => r.TenantId == null, ct);

        var admin = new User
        {
            TenantId = null,
            Email = email,
            FullName = fullName,
            PasswordHash = _passwordHasher.Hash(password),
            Status = UserStatus.Active,
            MustChangePassword = true,
            PasswordChangedAt = DateTimeOffset.UtcNow
        };
        await _context.Users.AddAsync(admin, ct);
        await _context.UserRoles.AddAsync(new UserRole { UserId = admin.Id, RoleId = role.Id }, ct);
        await _context.UserScopes.AddAsync(new UserScope { UserId = admin.Id, ScopeLevel = "PLATFORM", IsActive = true }, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogWarning("Seeded platform admin {Email}. Change the password immediately.", email);
    }
}
