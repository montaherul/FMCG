using Microsoft.EntityFrameworkCore;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Entities.Audit;
using TobaccoSaaS.Domain.Entities.Identity;
using TobaccoSaaS.Domain.Entities.Organization;
using TobaccoSaaS.Domain.Entities.Tenancy;

namespace TobaccoSaaS.Infrastructure.Data;

/// <summary>
/// The single EF Core context (spec §13). Tenant isolation and soft-delete are enforced
/// here as global query filters so no query can accidentally cross a tenant boundary
/// (spec §3.3, §18.1). Platform-scope callers (TenantId == null) bypass the tenant filter.
/// </summary>
public sealed class AppDbContext : DbContext
{
    private readonly IRequestContext? _requestContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, IRequestContext? requestContext = null)
        : base(options)
    {
        _requestContext = requestContext;
    }

    /// <summary>Tenant used by the global filters. Null = platform scope / seeding / design time.</summary>
    private Guid? CurrentTenantId => _requestContext?.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    public DbSet<TenantFeature> TenantFeatures => Set<TenantFeature>();

    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<UserScope> UserScopes => Set<UserScope>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();

    public DbSet<Position> Positions => Set<Position>();

    public DbSet<EmployeePosition> EmployeePositions => Set<EmployeePosition>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // IsDeleted is a computed convenience property, never persisted.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(BaseEntity.IsDeleted));
            }
        }

        ApplySoftDeleteFilters(modelBuilder);
        ApplyTenantFilters(modelBuilder);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Plan>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<FeatureFlag>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<User>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<UserSession>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<UserRole>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<UserScope>().HasQueryFilter(e => e.DeletedAt == null);
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>()
            .HasQueryFilter(e => e.DeletedAt == null && (CurrentTenantId == null || e.TenantId == CurrentTenantId));
        modelBuilder.Entity<TenantFeature>()
            .HasQueryFilter(e => e.DeletedAt == null && (CurrentTenantId == null || e.TenantId == CurrentTenantId));
        modelBuilder.Entity<TenantSetting>()
            .HasQueryFilter(e => e.DeletedAt == null && (CurrentTenantId == null || e.TenantId == CurrentTenantId));

        // Roles are tenant-scoped by nullable TenantId: platform roles (null) are visible
        // only to platform scope, tenant roles only to their own tenant (spec §40).
        modelBuilder.Entity<Role>()
            .HasQueryFilter(e => e.DeletedAt == null && (CurrentTenantId == null || e.TenantId == CurrentTenantId));

        // Users share one global namespace (unique email); tenant callers only ever see their own.
        modelBuilder.Entity<User>()
            .HasQueryFilter(e => e.DeletedAt == null && (CurrentTenantId == null || e.TenantId == CurrentTenantId));

        // Organization module: tenant-owned, soft-deleted rows hidden and cross-tenant reads blocked
        // centrally (spec §3.3, §18.1, §5.4 tenant isolation).
        modelBuilder.Entity<Employee>()
            .HasQueryFilter(e => e.DeletedAt == null && (CurrentTenantId == null || e.TenantId == CurrentTenantId));
        modelBuilder.Entity<OrgUnit>()
            .HasQueryFilter(e => e.DeletedAt == null && (CurrentTenantId == null || e.TenantId == CurrentTenantId));
        modelBuilder.Entity<Position>()
            .HasQueryFilter(e => e.DeletedAt == null && (CurrentTenantId == null || e.TenantId == CurrentTenantId));
        modelBuilder.Entity<EmployeePosition>()
            .HasQueryFilter(e => e.DeletedAt == null && (CurrentTenantId == null || e.TenantId == CurrentTenantId));

        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInfo()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.CreatedBy ??= _requestContext?.UserId;
                    entry.Entity.UpdatedBy ??= _requestContext?.UserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy ??= _requestContext?.UserId;
                    entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditLog>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }
    }
}
