using TobaccoSaaS.Domain.Common;

namespace TobaccoSaaS.Domain.Entities.Tenancy;

/// <summary>Per-tenant feature-flag override (spec §3.4: tenant can disable a module without errors).</summary>
public class TenantFeature : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid FeatureFlagId { get; set; }

    public bool Enabled { get; set; }

    public DateTimeOffset? EnabledAt { get; set; }

    public Tenant? Tenant { get; set; }

    public FeatureFlag? FeatureFlag { get; set; }
}
