using TobaccoSaaS.Domain.Common;

namespace TobaccoSaaS.Domain.Entities.Tenancy;

/// <summary>Platform-plane subscription plan (spec Appendix C: FREE/STARTER/BUSINESS/ENTERPRISE).</summary>
public class Plan : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Monthly price in BDT minor units (poisha). Money is never floating point (spec §18.1).</summary>
    public long PriceMonthlyMinor { get; set; }

    public int MaxUsers { get; set; }

    public int MaxOutlets { get; set; }

    public long MaxTransactionsPerYear { get; set; }

    public int StorageMb { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
