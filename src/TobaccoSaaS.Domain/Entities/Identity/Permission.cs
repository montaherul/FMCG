using TobaccoSaaS.Domain.Common;

namespace TobaccoSaaS.Domain.Entities.Identity;

/// <summary>Platform-wide permission catalog entry of the form <c>module.action</c> (Appendix B).</summary>
public class Permission : BaseEntity
{
    public string Module { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
