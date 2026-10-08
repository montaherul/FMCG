using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Application.Features.Rbac;

public sealed record RoleDto(Guid Id, string Code, string Name, RoleScope Scope, bool IsSystem, int PermissionCount);

public sealed record PermissionDto(string Module, string Code, string? Description);

public sealed record CreateRoleRequest(string Code, string Name, string? Description, IReadOnlyList<string> PermissionCodes);

public sealed record UpdateRolePermissionsRequest(IReadOnlyList<string> PermissionCodes);

public sealed record AssignRoleRequest(Guid UserId, Guid RoleId);

public sealed record SetUserScopeRequest(Guid UserId, Guid? ScopeNodeId, string ScopeLevel);

public sealed record UserSummaryDto(Guid Id, string Email, string FullName, UserStatus Status, IReadOnlyList<string> Roles, Guid? ScopeNodeId);

public sealed record CreateUserRequest(
    string Email,
    string FullName,
    string? Password,
    IReadOnlyList<Guid>? RoleIds);

public sealed record CreatedUserResult(Guid UserId, string Email, string? TemporaryPassword);
