using Microsoft.EntityFrameworkCore;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Domain.Entities.Identity;
using TobaccoSaaS.Infrastructure.Data;

namespace TobaccoSaaS.Infrastructure.Repositories;

/// <summary>
/// Specific repository for the identity/auth module (justified, AGENTS.md §16). Authentication
/// lookups must ignore tenant filters because login resolves a globally unique email.
/// </summary>
public sealed class IdentityRepository : IIdentityRepository
{
    private readonly AppDbContext _context;

    public IdentityRepository(AppDbContext context) => _context = context;

    public Task<User?> GetUserForAuthByEmailAsync(string email, CancellationToken ct = default)
        => _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Scopes)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null, ct);

    public Task<User?> GetUserForAuthByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Scopes)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);

    public Task<User?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Users
            .Include(u => u.Scopes)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<UserSession?> GetSessionByHashAsync(string refreshTokenHash, CancellationToken ct = default)
        => _context.UserSessions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash && s.DeletedAt == null, ct);

    public async Task<List<string>> GetRoleCodesAsync(Guid userId, CancellationToken ct = default)
        => await _context.UserRoles
            .Where(ur => ur.UserId == userId && ur.Role != null)
            .Select(ur => ur.Role!.Code)
            .Distinct()
            .ToListAsync(ct);

    public async Task<List<string>> GetPermissionCodesAsync(Guid userId, CancellationToken ct = default)
        => await (from ur in _context.UserRoles
                  join rp in _context.RolePermissions on ur.RoleId equals rp.RoleId
                  join p in _context.Permissions on rp.PermissionId equals p.Id
                  where ur.UserId == userId
                  select p.Code)
            .Distinct()
            .ToListAsync(ct);

    public Task<List<Role>> GetRolesByCodesAsync(IEnumerable<string> codes, Guid? tenantId, CancellationToken ct = default)
    {
        var set = codes.ToList();
        return _context.Roles
            .Where(r => set.Contains(r.Code) && r.TenantId == tenantId)
            .ToListAsync(ct);
    }

    public Task<List<User>> ListUsersAsync(Guid tenantId, CancellationToken ct = default)
        => _context.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email && u.DeletedAt == null, ct);

    public async Task AddUserAsync(User user, CancellationToken ct = default)
        => await _context.Users.AddAsync(user, ct);

    public async Task AddSessionAsync(UserSession session, CancellationToken ct = default)
        => await _context.UserSessions.AddAsync(session, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}

/// <summary>Append-only audit writer (spec §13.3). Persists immediately so denials are still recorded.</summary>
public sealed class AuditWriter : IAuditWriter
{
    private readonly AppDbContext _context;

    public AuditWriter(AppDbContext context) => _context = context;

    public async Task WriteAsync(Domain.Entities.Audit.AuditLog log, CancellationToken ct = default)
    {
        await _context.AuditLogs.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);
    }
}
