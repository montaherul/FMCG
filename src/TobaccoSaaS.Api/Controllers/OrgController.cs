using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TobaccoSaaS.Api.Security;
using TobaccoSaaS.Application.Common.Models;
using TobaccoSaaS.Application.Features.Organization;

namespace TobaccoSaaS.Api.Controllers;

/// <summary>
/// Organization module (spec §5, §20.3): org unit tree, positions with report-to links,
/// employee-position bindings and approver chain resolution. Routes match the spec catalog
/// under <c>/api/v1/org/*</c>.
/// </summary>
public sealed class OrgController : ApiControllerBase
{
    private readonly IOrganizationService _organization;
    private readonly IValidator<CreateOrgUnitRequest> _createUnitValidator;
    private readonly IValidator<UpdateOrgUnitRequest> _updateUnitValidator;
    private readonly IValidator<CreatePositionRequest> _createPositionValidator;
    private readonly IValidator<UpdatePositionRequest> _updatePositionValidator;
    private readonly IValidator<AssignEmployeePositionRequest> _assignValidator;

    public OrgController(
        IOrganizationService organization,
        IValidator<CreateOrgUnitRequest> createUnitValidator,
        IValidator<UpdateOrgUnitRequest> updateUnitValidator,
        IValidator<CreatePositionRequest> createPositionValidator,
        IValidator<UpdatePositionRequest> updatePositionValidator,
        IValidator<AssignEmployeePositionRequest> assignValidator)
    {
        _organization = organization;
        _createUnitValidator = createUnitValidator;
        _updateUnitValidator = updateUnitValidator;
        _createPositionValidator = createPositionValidator;
        _updatePositionValidator = updatePositionValidator;
        _assignValidator = assignValidator;
    }

    [HttpGet("units")]
    [HasPermission("org.view")]
    public async Task<ActionResult<ApiResponse<PagedResult<OrgUnitDto>>>> ListUnits(
        [FromQuery] string? q,
        [FromQuery] string? unitType,
        [FromQuery] Guid? parentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => Ok(ApiResponse<PagedResult<OrgUnitDto>>.Ok(
            await _organization.ListUnitsAsync(page, pageSize, q, unitType, parentId, ct)));

    [HttpGet("units/tree")]
    [HasPermission("org.view")]
    public async Task<ActionResult<ApiResponse<List<OrgUnitTreeNodeDto>>>> UnitTree(CancellationToken ct)
        => Ok(ApiResponse<List<OrgUnitTreeNodeDto>>.Ok(await _organization.GetUnitTreeAsync(ct)));

    [HttpGet("units/{id:guid}")]
    [HasPermission("org.view")]
    public async Task<ActionResult<ApiResponse<OrgUnitDto>>> GetUnit(Guid id, CancellationToken ct)
        => Ok(ApiResponse<OrgUnitDto>.Ok(await _organization.GetUnitAsync(id, ct)));

    [HttpPost("units")]
    [HasPermission("org.manage")]
    public async Task<ActionResult<ApiResponse<OrgUnitDto>>> CreateUnit([FromBody] CreateOrgUnitRequest request, CancellationToken ct)
    {
        await ValidateAsync(_createUnitValidator, request, ct);
        return Ok(ApiResponse<OrgUnitDto>.Ok(await _organization.CreateUnitAsync(request, ct)));
    }

    [HttpPut("units/{id:guid}")]
    [HasPermission("org.manage")]
    public async Task<ActionResult<ApiResponse<OrgUnitDto>>> UpdateUnit(Guid id, [FromBody] UpdateOrgUnitRequest request, CancellationToken ct)
    {
        await ValidateAsync(_updateUnitValidator, request, ct);
        return Ok(ApiResponse<OrgUnitDto>.Ok(await _organization.UpdateUnitAsync(id, request, ct)));
    }

    [HttpDelete("units/{id:guid}")]
    [HasPermission("org.manage")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUnit(Guid id, CancellationToken ct)
    {
        await _organization.DeleteUnitAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpGet("positions")]
    [HasPermission("org.view")]
    public async Task<ActionResult<ApiResponse<PagedResult<PositionDto>>>> ListPositions(
        [FromQuery] string? q,
        [FromQuery] Guid? orgUnitId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => Ok(ApiResponse<PagedResult<PositionDto>>.Ok(
            await _organization.ListPositionsAsync(page, pageSize, q, orgUnitId, isActive, ct)));

    [HttpGet("positions/{id:guid}")]
    [HasPermission("org.view")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> GetPosition(Guid id, CancellationToken ct)
        => Ok(ApiResponse<PositionDto>.Ok(await _organization.GetPositionAsync(id, ct)));

    [HttpPost("positions")]
    [HasPermission("org.manage")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> CreatePosition([FromBody] CreatePositionRequest request, CancellationToken ct)
    {
        await ValidateAsync(_createPositionValidator, request, ct);
        return Ok(ApiResponse<PositionDto>.Ok(await _organization.CreatePositionAsync(request, ct)));
    }

    [HttpPut("positions/{id:guid}")]
    [HasPermission("org.manage")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> UpdatePosition(Guid id, [FromBody] UpdatePositionRequest request, CancellationToken ct)
    {
        await ValidateAsync(_updatePositionValidator, request, ct);
        return Ok(ApiResponse<PositionDto>.Ok(await _organization.UpdatePositionAsync(id, request, ct)));
    }

    [HttpDelete("positions/{id:guid}")]
    [HasPermission("org.manage")]
    public async Task<ActionResult<ApiResponse<object>>> DeletePosition(Guid id, CancellationToken ct)
    {
        await _organization.DeletePositionAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("positions/{id:guid}/assign")]
    [HasPermission("org.manage")]
    public async Task<ActionResult<ApiResponse<EmployeePositionDto>>> AssignEmployee(Guid id, [FromBody] AssignEmployeePositionRequest request, CancellationToken ct)
    {
        await ValidateAsync(_assignValidator, request, ct);
        return Ok(ApiResponse<EmployeePositionDto>.Ok(await _organization.AssignEmployeeAsync(id, request, ct)));
    }

    [HttpGet("positions/{id:guid}/assignments")]
    [HasPermission("org.view")]
    public async Task<ActionResult<ApiResponse<List<EmployeePositionDto>>>> ListAssignments(Guid id, CancellationToken ct)
        => Ok(ApiResponse<List<EmployeePositionDto>>.Ok(await _organization.ListAssignmentsAsync(id, ct)));

    [HttpDelete("assignments/{id:guid}")]
    [HasPermission("org.manage")]
    public async Task<ActionResult<ApiResponse<object>>> ReleaseAssignment(Guid id, CancellationToken ct)
    {
        await _organization.ReleaseAssignmentAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpGet("chain/{positionId:guid}")]
    [HasPermission("org.view")]
    public async Task<ActionResult<ApiResponse<List<ApproverChainNodeDto>>>> ApproverChain(Guid positionId, CancellationToken ct)
        => Ok(ApiResponse<List<ApproverChainNodeDto>>.Ok(await _organization.GetApproverChainAsync(positionId, ct)));
}