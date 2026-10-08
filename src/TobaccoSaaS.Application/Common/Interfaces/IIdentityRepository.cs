using TobaccoSaaS.Domain.Entities.Audit;
using TobaccoSaaS.Domain.Entities.Identity;

namespace TobaccoSaaS.Application.Common.Interfaces;

/// <summary>
/// Specific data-access contract for the identity/auth module. Justified under AGENTS.md §16:
/// auth needs eager-loaded role/permission graphs that the generic repository cannot express.
/// </summary>
public interface IIdentityRepository
{
    Task<User?> GetUserForAuthByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> GetUserForAuthByIdAsync(Guid id, CancellationToken ct = default);

    Task<User?> GetUserByIdAsync(Guid id, CancellationToken ct = default);

    Task<UserSession?> GetSessionByHashAsync(string refreshTokenHash, CancellationToken ct = default);

    Task<List<string>> GetRoleCodesAsync(Guid userId, CancellationToken ct = default);

    Task<List<string>> GetPermissionCodesAsync(Guid userId, CancellationToken ct = default);

    Task<List<Role>> GetRolesByCodesAsync(IEnumerable<string> codes, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Global (cross-tenant) email check for the globally unique login identity.</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    Task<List<User>> ListUsersAsync(Guid tenantId, CancellationToken ct = default);

    Task AddUserAsync(User user, CancellationToken ct = default);

    Task AddSessionAsync(UserSession session, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Append-only audit writer (spec §13.3).</summary>
public interface IAuditWriter
{
    Task WriteAsync(AuditLog log, CancellationToken ct = default);
}
