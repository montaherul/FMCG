using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Domain.Entities.Tenancy;

/// <summary>Platform-plane table: an onboarded tenant company (spec §3.4, §5).</summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique tenant slug used for resolution and provisioning.</summary>
    public string Slug { get; set; } = string.Empty;

    public TenantStatus Status { get; set; } = TenantStatus.Trial;

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public string Country { get; set; } = "BD";

    public string Timezone { get; set; } = "Asia/Dhaka";

    public string Currency { get; set; } = "BDT";

    public ICollection<TenantSetting> Settings { get; set; } = new List<TenantSetting>();

    public ICollection<TenantFeature> Features { get; set; } = new List<TenantFeature>();

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
