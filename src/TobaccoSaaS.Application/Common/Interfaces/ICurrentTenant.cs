namespace TobaccoSaaS.Application.Common.Interfaces;

/// <summary>
/// Minimal, dependency-free request context used by the data layer for global query filters
/// (tenant isolation) and for stamping audit columns. It is intentionally separate from
/// <see cref="ICurrentUserService"/>: the full user service depends on repositories, which
/// depend on the DbContext, so using it here would create a DI cycle.
/// </summary>
public interface IRequestContext
{
    /// <summary>Current tenant id, or null for platform scope / unauthenticated / design time.</summary>
    Guid? TenantId { get; }

    /// <summary>Current user id, used to stamp CreatedBy/UpdatedBy.</summary>
    Guid? UserId { get; }

    bool IsPlatformScope { get; }
}
