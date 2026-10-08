using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobaccoSaaS.Domain.Entities.Tenancy;

namespace TobaccoSaaS.Infrastructure.Data.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ContactEmail).HasMaxLength(320);
        b.Property(x => x.ContactPhone).HasMaxLength(32);
        b.Property(x => x.Country).HasMaxLength(2).IsRequired();
        b.Property(x => x.Timezone).HasMaxLength(64).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();

        b.HasIndex(x => x.Slug).IsUnique();

        b.HasMany(x => x.Settings).WithOne(x => x.Tenant!).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Features).WithOne(x => x.Tenant!).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Subscriptions).WithOne(x => x.Tenant!).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.ToTable("plans");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();

        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.PlanId);

        b.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> b)
    {
        b.ToTable("feature_flags");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(80).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Category).HasMaxLength(80).IsRequired();
        b.Property(x => x.ModuleKey).HasMaxLength(80);
        b.Property(x => x.DefaultState).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class TenantFeatureConfiguration : IEntityTypeConfiguration<TenantFeature>
{
    public void Configure(EntityTypeBuilder<TenantFeature> b)
    {
        b.ToTable("tenant_features");
        b.HasKey(x => x.Id);

        b.HasIndex(x => new { x.TenantId, x.FeatureFlagId }).IsUnique();

        b.HasOne(x => x.FeatureFlag).WithMany().HasForeignKey(x => x.FeatureFlagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TenantSettingConfiguration : IEntityTypeConfiguration<TenantSetting>
{
    public void Configure(EntityTypeBuilder<TenantSetting> b)
    {
        b.ToTable("tenant_settings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(120).IsRequired();
        b.Property(x => x.ValueJson).IsRequired();
        b.Property(x => x.Group).HasMaxLength(80).IsRequired();

        b.HasIndex(x => new { x.TenantId, x.Key }).IsUnique();
    }
}
