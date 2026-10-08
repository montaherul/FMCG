using TobaccoSaaS.Domain.Common;

namespace TobaccoSaaS.Domain.Entities.Identity;

/// <summary>Join table user_roles. Role assignment is audited (spec §4.2).</summary>
public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? AssignedBy { get; set; }

    public User? User { get; set; }

    public Role? Role { get; set; }
}
