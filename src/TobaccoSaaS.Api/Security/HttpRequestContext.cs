using TobaccoSaaS.Application.Common.Interfaces;

namespace TobaccoSaaS.Api.Security;

/// <summary>
/// Dependency-free request context consumed by <c>AppDbContext</c> for tenant filters and
/// audit-column stamping. Reads only validated JWT claims (spec §3.3.1, §18.2).
/// </summary>
public sealed class HttpRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpRequestContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? TenantId => ParseGuid("tenant_id");

    public Guid? UserId => ParseGuid("sub");

    public bool IsPlatformScope => string.Equals(
        _accessor.HttpContext?.User.FindFirst("is_platform")?.Value,
        "true",
        StringComparison.OrdinalIgnoreCase);

    private Guid? ParseGuid(string claim)
        => Guid.TryParse(_accessor.HttpContext?.User.FindFirst(claim)?.Value, out var id) ? id : null;
}
