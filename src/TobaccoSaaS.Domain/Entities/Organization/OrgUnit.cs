using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Domain.Entities.Organization;

/// <summary>
/// A node in a tenant's organization tree: organization root, business unit or department
/// (spec §5.1, §19.4). Nesting and cycle rules are enforced by the application service.
/// </summary>
public class OrgUnit : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public OrgUnitType UnitType { get; set; }

    public Guid? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public OrgUnit? Parent { get; set; }

    public ICollection<OrgUnit> Children { get; set; } = new List<OrgUnit>();

    public ICollection<Position> Positions { get; set; } = new List<Position>();
}