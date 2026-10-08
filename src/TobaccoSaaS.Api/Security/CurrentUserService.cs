using System.Security.Claims;
using TobaccoSaaS.Application.Common.Interfaces;

namespace TobaccoSaaS.Api.Security;

/// <summary>
/// Reads identity, tenant and scope from the validated JWT only. Nothing here trusts the
/// request body, so a client can never widen its tenant scope (spec §3.3.1, §17).
/// Permissions are resolved per request for immediate effect (spec §4.4.1).
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    private readonly IIdentityRepository _identity;
    private IReadOnlyCollection<string>? _permissions;

    public CurrentUserService(IHttpContextAccessor accessor, IIdentityRepository identity)
    {
        _accessor = accessor;
        _identity = identity;
    }

    public Guid? UserId => ParseGuid(Claim("sub"));

    public Guid? TenantId => ParseGuid(Claim("tenant_id"));

    public string? Email => Claim("email");

    public bool IsAuthenticated => _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsPlatformScope => string.Equals(Claim("is_platform"), "true", StringComparison.OrdinalIgnoreCase);

    public Guid? ScopeNodeId => ParseGuid(Claim("scope_node_id"));

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public IReadOnlyCollection<string> Roles =>
        _accessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        ?? Array.Empty<string>();

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(CancellationToken ct = default)
    {
        if (_permissions is not null)
        {
            return _permissions;
        }

        if (UserId is null)
        {
            return Array.Empty<string>();
        }

        _permissions = await _identity.GetPermissionCodesAsync(UserId.Value, ct);
        return _permissions;
    }

    private string? Claim(string type) => _accessor.HttpContext?.User.FindFirst(type)?.Value;

    private static Guid? ParseGuid(string? value)
        => Guid.TryParse(value, out var id) ? id : null;
}
