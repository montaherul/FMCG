using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Application.Features.Tenancy;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    TenantStatus Status,
    string Country,
    string Timezone,
    string? PlanCode,
    DateTimeOffset CreatedAt);

public sealed record CreateTenantRequest(
    string Name,
    string Slug,
    string AdminEmail,
    string AdminFullName,
    string PlanCode = "TRIAL");

public sealed record UpdateTenantStatusRequest(TenantStatus Status);

public sealed record TenantSettingDto(string Key, string ValueJson, string Group);

public sealed record UpsertSettingRequest(string Key, string ValueJson, string Group = "General");

public sealed record TenantFeatureDto(Guid FeatureFlagId, string Code, string Name, bool Enabled, FeatureFlagState DefaultState);

public sealed record SetFeatureRequest(string Code, bool Enabled);

public sealed record ProvisionTenantResult(TenantDto Tenant, string AdminEmail, string TemporaryPassword);
