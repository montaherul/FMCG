using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Domain.Entities.Tenancy;

/// <summary>Global feature-flag catalogue entry (platform plane, spec §2.4 / §3.4).</summary>
public class FeatureFlag : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Category { get; set; } = "General";

    public FeatureFlagState DefaultState { get; set; } = FeatureFlagState.Disabled;

    /// <summary>Modules whose UI should be hidden when this flag is disabled (spec §7).</summary>
    public string? ModuleKey { get; set; }
}
