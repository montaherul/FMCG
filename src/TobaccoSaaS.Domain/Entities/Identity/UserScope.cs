using TobaccoSaaS.Domain.Common;

namespace TobaccoSaaS.Domain.Entities.Identity;

/// <summary>
/// Single active geographic scope node per user (spec §4.4.1). The FK to geo_nodes is
/// introduced by the Geography module (build step 07); the node id is stored now so the
/// authorization ladder can be wired without a later schema shock.
/// </summary>
public class UserScope : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid? ScopeNodeId { get; set; }

    /// <summary>DIVISION / REGION / AREA / TERRITORY / ROUTE / DISTRIBUTOR (spec §4.3).</summary>
    public string ScopeLevel { get; set; } = "TENANT";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset EffectiveFrom { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
}
