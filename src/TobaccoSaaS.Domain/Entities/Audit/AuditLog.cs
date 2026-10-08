namespace TobaccoSaaS.Domain.Entities.Audit;

/// <summary>
/// Append-only audit trail (spec §13.3). Deliberately not derived from BaseEntity:
/// audit rows are never soft-deleted and never updated; the DB role has no
/// UPDATE/DELETE on this table (tamper evidence).
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    /// <summary>e.g. auth.login, auth.login.failed, role.assign, tenant.provision.</summary>
    public string Action { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
