namespace TobaccoSaaS.Application.Features.Rbac;

public interface IRbacService
{
    Task<List<RoleDto>> ListRolesAsync(CancellationToken ct = default);

    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default);

    Task<RoleDto> UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken ct = default);

    Task<List<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default);

    Task<List<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default);

    Task<List<UserSummaryDto>> ListUsersAsync(CancellationToken ct = default);

    Task<CreatedUserResult> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);

    Task AssignRoleAsync(AssignRoleRequest request, CancellationToken ct = default);

    Task SetUserScopeAsync(SetUserScopeRequest request, CancellationToken ct = default);
}
