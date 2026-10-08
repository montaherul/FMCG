using Microsoft.AspNetCore.Authorization;

namespace TobaccoSaaS.Api.Security;

/// <summary>
/// Declarative endpoint authorization by permission code, e.g. <c>[HasPermission("tenant.create")]</c>
/// (spec §5.2). Resolved dynamically by <see cref="PermissionPolicyProvider"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public HasPermissionAttribute(string permission) => Policy = PolicyPrefix + permission;
}
