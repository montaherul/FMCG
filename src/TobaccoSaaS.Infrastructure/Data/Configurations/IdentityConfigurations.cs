using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Entities.Identity;

namespace TobaccoSaaS.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(320).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(32);
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.MfaSecret).HasMaxLength(128);
        b.Property(x => x.ResetTokenHash).HasMaxLength(128);

        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.TenantId);

        b.HasMany(x => x.UserRoles).WithOne(x => x.User!).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Scopes).WithOne(x => x.User!).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Sessions).WithOne(x => x.User!).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> b)
    {
        b.ToTable("user_sessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.RefreshTokenHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.ReplacedByHash).HasMaxLength(128);
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(512);
        b.Property(x => x.DeviceLabel).HasMaxLength(100);

        b.HasIndex(x => x.RefreshTokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Scope).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Module).HasMaxLength(100).IsRequired();
        b.Property(x => x.Code).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(300);

        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permissions");
        b.HasKey(x => new { x.RoleId, x.PermissionId });

        b.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.PermissionId);
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("user_roles");
        b.HasKey(x => x.Id);

        b.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
    }
}

public sealed class UserScopeConfiguration : IEntityTypeConfiguration<UserScope>
{
    public void Configure(EntityTypeBuilder<UserScope> b)
    {
        b.ToTable("user_scopes");
        b.HasKey(x => x.Id);
        b.Property(x => x.ScopeLevel).HasMaxLength(30).IsRequired();

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.ScopeNodeId);
    }
}

/// <summary>HR identity table (spec §19.3). Shipped with the organization migration because
/// <c>employee_positions</c> references it (spec group order: identity before organization).</summary>
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("employees");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.PhotoKey).HasMaxLength(260);
        b.Property(x => x.NationalId).HasMaxLength(40);
        b.Property(x => x.Phone).HasMaxLength(32).IsRequired();
        b.Property(x => x.Email).HasMaxLength(320);
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.Gender)
            .HasConversion(
                v => v.HasValue ? SpecVocabulary.GenderToDb(v.Value) : null,
                s => s == null ? null : SpecVocabulary.GenderFromDb(s))
            .HasMaxLength(10);
        b.Property(x => x.Status)
            .HasConversion(
                v => SpecVocabulary.EmployeeStatusToDb(v),
                s => SpecVocabulary.EmployeeStatusFromDb(s))
            .HasMaxLength(20)
            .IsRequired();

        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Spec §19.3 unique indexes (soft-delete filtered) + one user account per employee.
        b.HasIndex(x => new { x.TenantId, x.EmployeeCode }).IsUnique()
            .HasDatabaseName("uq_employees_code").HasFilter("deleted_at IS NULL");
        b.HasIndex(x => new { x.TenantId, x.Phone }).IsUnique()
            .HasDatabaseName("uq_employees_phone").HasFilter("deleted_at IS NULL");
        b.HasIndex(x => x.UserId).IsUnique();

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_employees_status",
                "status IN ('ACTIVE', 'INACTIVE', 'SUSPENDED', 'TERMINATED', 'ON_LEAVE')");
            t.HasCheckConstraint("ck_employees_gender",
                "gender IS NULL OR gender IN ('MALE', 'FEMALE', 'OTHER')");
        });
    }
}
