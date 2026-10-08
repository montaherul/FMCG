namespace TobaccoSaaS.Application.Common.Interfaces;

/// <summary>
/// Server-derived identity context. Tenant and scope are never read from the client
/// request body (spec §3.3.1, §17). Permissions are resolved per request so role/scope
/// changes take effect on the next request (spec §4.4.1).
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    Guid? TenantId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    /// <summary>True for platform-plane users; they have no tenant scope.</summary>
    bool IsPlatformScope { get; }

    Guid? ScopeNodeId { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }

    IReadOnlyCollection<string> Roles { get; }

    Task<IReadOnlyCollection<string>> GetPermissionsAsync(CancellationToken ct = default);
}
