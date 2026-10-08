namespace TobaccoSaaS.Domain.Entities.Identity;

/// <summary>Join table role_permissions (spec §18.1).</summary>
public class RolePermission
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public Role? Role { get; set; }

    public Permission? Permission { get; set; }
}
