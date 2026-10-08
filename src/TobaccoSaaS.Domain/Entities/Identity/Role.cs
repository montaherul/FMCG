using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Domain.Entities.Identity;

/// <summary>
/// A named bundle of permissions. Tenant roles carry TenantId; platform roles have
/// <c>TenantId == null</c> and must never be seeded per tenant (spec §4.3, §40.1).
/// </summary>
public class Role : BaseEntity
{
    public Guid? TenantId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public RoleScope Scope { get; set; } = RoleScope.Tenant;

    public bool IsSystem { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
