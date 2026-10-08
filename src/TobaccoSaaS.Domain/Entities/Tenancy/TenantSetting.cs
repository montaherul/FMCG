using TobaccoSaaS.Domain.Common;

namespace TobaccoSaaS.Domain.Entities.Tenancy;

/// <summary>Key/value tenant configuration (JSONB payload for flexible settings, spec §18.1).</summary>
public class TenantSetting : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string ValueJson { get; set; } = "{}";

    public string Group { get; set; } = "General";

    public Tenant? Tenant { get; set; }
}
