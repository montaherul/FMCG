using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Entities.Identity;

namespace TobaccoSaaS.Domain.Entities.Organization;

/// <summary>
/// Effective-dated binding of an employee to a position. An employee holds at most one
/// primary position at a time; secondary (acting) positions are flagged and validity-scoped
/// (spec §5.3, §19.4).
/// </summary>
public class EmployeePosition : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid EmployeeId { get; set; }

    public Guid PositionId { get; set; }

    public bool IsPrimary { get; set; } = true;

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public Employee? Employee { get; set; }

    public Position? Position { get; set; }
}