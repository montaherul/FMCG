using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Entities.Identity;

namespace TobaccoSaaS.Domain.Entities.Organization;

/// <summary>
/// A seat in the organization, e.g. "Area Manager, South Chattogram". Reports to another
/// position, carries an optional default role and is occupied over time via
/// <see cref="EmployeePosition"/> (spec §5.1, §19.4). Report-to links must stay acyclic.
/// </summary>
public class Position : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid OrgUnitId { get; set; }

    public string Title { get; set; } = string.Empty;

    public Guid? ReportsTo { get; set; }

    public Guid? DefaultRoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public OrgUnit? OrgUnit { get; set; }

    public Position? ReportsToPosition { get; set; }

    public Role? DefaultRole { get; set; }

    public ICollection<Position> DirectReports { get; set; } = new List<Position>();

    public ICollection<EmployeePosition> EmployeePositions { get; set; } = new List<EmployeePosition>();
}