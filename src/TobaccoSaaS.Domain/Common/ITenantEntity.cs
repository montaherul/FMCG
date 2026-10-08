namespace TobaccoSaaS.Domain.Common;

/// <summary>
/// Marker for tenant-owned tables. Every such table carries a NOT NULL tenant_id
/// and is filtered centrally at the data-access layer (spec §3.3, §18.1).
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
