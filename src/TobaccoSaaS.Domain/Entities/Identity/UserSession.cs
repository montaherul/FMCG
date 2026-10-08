using TobaccoSaaS.Domain.Common;

namespace TobaccoSaaS.Domain.Entities.Identity;

/// <summary>
/// A refresh-token session used for rotation, idle timeout and immediate revocation
/// (spec §4.1, §6.1 "TERMINATED/INACTIVE must immediately revoke sessions and tokens").
/// </summary>
public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }

    public string RefreshTokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? ReplacedByHash { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? DeviceLabel { get; set; }

    public User? User { get; set; }
}
