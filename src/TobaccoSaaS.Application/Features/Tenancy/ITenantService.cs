using TobaccoSaaS.Application.Common.Models;

namespace TobaccoSaaS.Application.Features.Tenancy;

public interface ITenantService
{
    Task<PagedResult<TenantDto>> ListAsync(int page, int pageSize, CancellationToken ct = default);

    Task<TenantDto> GetAsync(Guid id, CancellationToken ct = default);

    Task<ProvisionTenantResult> ProvisionAsync(CreateTenantRequest request, CancellationToken ct = default);

    Task<TenantDto> UpdateStatusAsync(Guid id, UpdateTenantStatusRequest request, CancellationToken ct = default);

    Task<List<TenantSettingDto>> GetSettingsAsync(Guid tenantId, CancellationToken ct = default);

    Task UpsertSettingAsync(Guid tenantId, UpsertSettingRequest request, CancellationToken ct = default);

    Task<List<TenantFeatureDto>> GetFeaturesAsync(Guid tenantId, CancellationToken ct = default);

    Task SetFeatureAsync(Guid tenantId, SetFeatureRequest request, CancellationToken ct = default);
}
