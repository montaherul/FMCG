using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Entities.Organization;

namespace TobaccoSaaS.Infrastructure.Data.Configurations;

/// <summary>
/// Organization module tables (spec §19.4). Indexes and CHECK constraints follow the
/// contractual DDL; enum columns store the spec's UPPER_SNAKE vocabulary.
/// </summary>
public sealed class OrgUnitConfiguration : IEntityTypeConfiguration<OrgUnit>
{
    public void Configure(EntityTypeBuilder<OrgUnit> b)
    {
        b.ToTable("org_units");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Code).HasMaxLength(50);
        b.Property(x => x.UnitType)
            .HasConversion(
                v => SpecVocabulary.OrgUnitTypeToDb(v),
                s => SpecVocabulary.OrgUnitTypeFromDb(s))
            .HasMaxLength(20)
            .IsRequired();

        b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId);

        // ix_org_units_tenant_parent (spec §19.4)
        b.HasIndex(x => new { x.TenantId, x.ParentId }).HasDatabaseName("ix_org_units_tenant_parent");
        b.ToTable(t => t.HasCheckConstraint(
            "ck_org_units_unit_type",
            "unit_type IN ('ORGANIZATION', 'BUSINESS_UNIT', 'DEPARTMENT')"));
    }
}

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> b)
    {
        b.ToTable("positions");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.OrgUnitId).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();

        b.HasOne(x => x.OrgUnit).WithMany(x => x.Positions).HasForeignKey(x => x.OrgUnitId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.ReportsToPosition).WithMany(x => x.DirectReports).HasForeignKey(x => x.ReportsTo)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.DefaultRole).WithMany().HasForeignKey(x => x.DefaultRoleId)
            .OnDelete(DeleteBehavior.NoAction);

        // Spec §19.4 index contract + the tenant-leading index rule (spec §18.1).
        b.HasIndex(x => x.OrgUnitId).HasDatabaseName("ix_positions_unit").HasFilter("is_active");
        b.HasIndex(x => x.ReportsTo).HasDatabaseName("ix_positions_reports");
        b.HasIndex(x => x.TenantId);
    }
}

public sealed class EmployeePositionConfiguration : IEntityTypeConfiguration<EmployeePosition>
{
    public void Configure(EntityTypeBuilder<EmployeePosition> b)
    {
        b.ToTable("employee_positions");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.EmployeeId).IsRequired();
        b.Property(x => x.PositionId).IsRequired();
        b.Property(x => x.ValidFrom).HasColumnType("date").IsRequired();
        b.Property(x => x.ValidTo).HasColumnType("date");

        b.HasOne(x => x.Employee).WithMany(x => x.EmployeePositions).HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Position).WithMany(x => x.EmployeePositions).HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.NoAction);

        // ix_emp_pos_employee (spec §19.4) + the tenant-leading index rule (spec §18.1).
        b.HasIndex(x => new { x.EmployeeId, x.ValidFrom }).HasDatabaseName("ix_emp_pos_employee");
        b.HasIndex(x => x.TenantId);
    }
}