using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Domain.Entities.Identity;

/// <summary>
/// A credential identity. Tenant users carry a TenantId; platform-plane users have
/// <c>TenantId == null</c> (spec §40). Login is resolved from a globally unique email.
/// </summary>
public class User : BaseEntity
{
    public Guid? TenantId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public bool IsMfaEnabled { get; set; }

    public string? MfaSecret { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset? PasswordChangedAt { get; set; }

    public bool MustChangePassword { get; set; }

    public string? ResetTokenHash { get; set; }

    public DateTimeOffset? ResetTokenExpiresAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<UserScope> Scopes { get; set; } = new List<UserScope>();

    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
