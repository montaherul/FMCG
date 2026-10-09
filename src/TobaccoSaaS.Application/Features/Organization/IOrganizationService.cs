using TobaccoSaaS.Application.Common.Models;

namespace TobaccoSaaS.Application.Features.Organization;

/// <summary>
/// Organization module use cases: org tree CRUD, position CRUD with report-to cycle
/// prevention, employee-position bindings and approver chain resolution (spec §5, §20.3).
/// All operations are tenant-scoped server-side; the client supplies no tenant identifier.
/// </summary>
public interface IOrganizationService
{
    Task<PagedResult<OrgUnitDto>> ListUnitsAsync(int page, int pageSize, string? q, string? unitType, Guid? parentId, CancellationToken ct = default);

    Task<List<OrgUnitTreeNodeDto>> GetUnitTreeAsync(CancellationToken ct = default);

    Task<OrgUnitDto> GetUnitAsync(Guid id, CancellationToken ct = default);

    Task<OrgUnitDto> CreateUnitAsync(CreateOrgUnitRequest request, CancellationToken ct = default);

    Task<OrgUnitDto> UpdateUnitAsync(Guid id, UpdateOrgUnitRequest request, CancellationToken ct = default);

    Task DeleteUnitAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<PositionDto>> ListPositionsAsync(int page, int pageSize, string? q, Guid? orgUnitId, bool? isActive, CancellationToken ct = default);

    Task<PositionDto> GetPositionAsync(Guid id, CancellationToken ct = default);

    Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, CancellationToken ct = default);

    Task<PositionDto> UpdatePositionAsync(Guid id, UpdatePositionRequest request, CancellationToken ct = default);

    Task DeletePositionAsync(Guid id, CancellationToken ct = default);

    Task<EmployeePositionDto> AssignEmployeeAsync(Guid positionId, AssignEmployeePositionRequest request, CancellationToken ct = default);

    Task<List<EmployeePositionDto>> ListAssignmentsAsync(Guid positionId, CancellationToken ct = default);

    Task ReleaseAssignmentAsync(Guid id, CancellationToken ct = default);

    Task<List<ApproverChainNodeDto>> GetApproverChainAsync(Guid positionId, CancellationToken ct = default);
}