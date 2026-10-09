using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Domain.Entities.Identity;

/// <summary>
/// An HR identity independent of any login; may exist without a user account. Positions held
/// over time are tracked via <c>employee_position</c> (spec §5.1, §19.3). One-to-one with an
/// optional <see cref="User"/> account; disabled accounts never delete the employee.
/// </summary>
public class Employee : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? PhotoKey { get; set; }

    public Gender? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? NationalId { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateOnly JoiningDate { get; set; }

    public Guid? UserId { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public User? User { get; set; }

    public ICollection<Organization.EmployeePosition> EmployeePositions { get; set; } = new List<Organization.EmployeePosition>();
}