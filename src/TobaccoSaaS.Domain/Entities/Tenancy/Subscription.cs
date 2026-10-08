using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Domain.Entities.Tenancy;

/// <summary>Binds a tenant to a plan and carries its lifecycle state (spec build step 25).</summary>
public class Subscription : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid PlanId { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? TrialEndsAt { get; set; }

    public DateTimeOffset? CurrentPeriodEnd { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public Tenant? Tenant { get; set; }

    public Plan? Plan { get; set; }
}
