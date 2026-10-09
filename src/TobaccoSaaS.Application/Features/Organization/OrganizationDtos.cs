namespace TobaccoSaaS.Application.Features.Organization;

public sealed record OrgUnitDto(Guid Id, string UnitType, Guid? ParentId, string Name, string? Code, DateTimeOffset CreatedAt);

public sealed record OrgUnitTreeNodeDto(Guid Id, string UnitType, Guid? ParentId, string Name, string? Code, IReadOnlyList<OrgUnitTreeNodeDto> Children);

public sealed record CreateOrgUnitRequest(string UnitType, Guid? ParentId, string Name, string? Code);

public sealed record UpdateOrgUnitRequest(string UnitType, Guid? ParentId, string Name, string? Code);

public sealed record PositionDto(Guid Id, Guid OrgUnitId, string Title, Guid? ReportsTo, Guid? DefaultRoleId, bool IsActive, DateTimeOffset CreatedAt);

public sealed record CreatePositionRequest(Guid OrgUnitId, string Title, Guid? ReportsTo, Guid? DefaultRoleId);

public sealed record UpdatePositionRequest(Guid OrgUnitId, string Title, Guid? ReportsTo, Guid? DefaultRoleId, bool IsActive);

public sealed record AssignEmployeePositionRequest(Guid EmployeeId, bool IsPrimary, DateOnly ValidFrom, DateOnly? ValidTo);

public sealed record EmployeePositionDto(Guid Id, Guid EmployeeId, string EmployeeName, Guid PositionId, bool IsPrimary, DateOnly ValidFrom, DateOnly? ValidTo);

public sealed record ApproverChainNodeDto(Guid PositionId, string Title, Guid? ReportsTo, bool IsVacant);