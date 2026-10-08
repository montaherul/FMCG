namespace TobaccoSaaS.Domain.Enums;

/// <summary>Tenant lifecycle (spec §3.4 Tenancy module).</summary>
public enum TenantStatus
{
    Active = 0,
    Suspended = 1,
    Trial = 2,
    Terminated = 3
}

/// <summary>Plan/subscription states (spec build step 25).</summary>
public enum SubscriptionStatus
{
    Trialing = 0,
    Active = 1,
    PastDue = 2,
    Cancelled = 3,
    Suspended = 4
}

/// <summary>User account status (spec §6.1).</summary>
public enum UserStatus
{
    Active = 0,
    Inactive = 1,
    Suspended = 2,
    Terminated = 3
}

/// <summary>
/// Authorization plane. Platform roles never inherit tenant scope (spec §4.1, §4.4.5).
/// </summary>
public enum RoleScope
{
    Platform = 0,
    Tenant = 1
}

/// <summary>Global feature flag catalogue state (spec §3.4 Tenancy, build step 02).</summary>
public enum FeatureFlagState
{
    Disabled = 0,
    Enabled = 1,
    Preview = 2
}
