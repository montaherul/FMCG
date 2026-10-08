using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TobaccoSaaS.Api.Security;
using TobaccoSaaS.Application.Common.Models;
using TobaccoSaaS.Application.Features.Tenancy;

namespace TobaccoSaaS.Api.Controllers;

/// <summary>Platform-plane tenant administration. Every endpoint requires a platform permission.</summary>
public sealed class TenantsController : ApiControllerBase
{
    private readonly ITenantService _tenants;
    private readonly IValidator<CreateTenantRequest> _createValidator;

    public TenantsController(ITenantService tenants, IValidator<CreateTenantRequest> createValidator)
    {
        _tenants = tenants;
        _createValidator = createValidator;
    }

    [HttpGet]
    [HasPermission("platform.tenant.view")]
    public async Task<ActionResult<ApiResponse<PagedResult<TenantDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => Ok(ApiResponse<PagedResult<TenantDto>>.Ok(await _tenants.ListAsync(page, pageSize, ct)));

    [HttpGet("{id:guid}")]
    [HasPermission("platform.tenant.view")]
    public async Task<ActionResult<ApiResponse<TenantDto>>> Get(Guid id, CancellationToken ct)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.GetAsync(id, ct)));

    [HttpPost]
    [HasPermission("platform.tenant.create")]
    public async Task<ActionResult<ApiResponse<ProvisionTenantResult>>> Provision([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        await ValidateAsync(_createValidator, request, ct);
        return Ok(ApiResponse<ProvisionTenantResult>.Ok(await _tenants.ProvisionAsync(request, ct)));
    }

    [HttpPatch("{id:guid}/status")]
    [HasPermission("platform.tenant.suspend")]
    public async Task<ActionResult<ApiResponse<TenantDto>>> UpdateStatus(Guid id, [FromBody] UpdateTenantStatusRequest request, CancellationToken ct)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.UpdateStatusAsync(id, request, ct)));

    [HttpGet("{id:guid}/settings")]
    [HasPermission("platform.tenant.view")]
    public async Task<ActionResult<ApiResponse<List<TenantSettingDto>>>> GetSettings(Guid id, CancellationToken ct)
        => Ok(ApiResponse<List<TenantSettingDto>>.Ok(await _tenants.GetSettingsAsync(id, ct)));

    [HttpPut("{id:guid}/settings")]
    [HasPermission("platform.tenant.plan")]
    public async Task<ActionResult<ApiResponse<object>>> UpsertSetting(Guid id, [FromBody] UpsertSettingRequest request, CancellationToken ct)
    {
        await _tenants.UpsertSettingAsync(id, request, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpGet("{id:guid}/features")]
    [HasPermission("platform.tenant.view")]
    public async Task<ActionResult<ApiResponse<List<TenantFeatureDto>>>> GetFeatures(Guid id, CancellationToken ct)
        => Ok(ApiResponse<List<TenantFeatureDto>>.Ok(await _tenants.GetFeaturesAsync(id, ct)));

    [HttpPut("{id:guid}/features")]
    [HasPermission("platform.tenant.features")]
    public async Task<ActionResult<ApiResponse<object>>> SetFeature(Guid id, [FromBody] SetFeatureRequest request, CancellationToken ct)
    {
        await _tenants.SetFeatureAsync(id, request, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
