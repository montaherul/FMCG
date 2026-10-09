using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TobaccoSaaS.Api.Security;
using TobaccoSaaS.Application.Common.Models;
using TobaccoSaaS.Application.Features.Rbac;

namespace TobaccoSaaS.Api.Controllers;

/// <summary>Tenant-scoped RBAC: role catalog and per-role permission matrix (spec §4.2, §4.3).</summary>
public sealed class RolesController : ApiControllerBase
{
    private readonly IRbacService _rbac;
    private readonly IValidator<CreateRoleRequest> _createValidator;

    public RolesController(IRbacService rbac, IValidator<CreateRoleRequest> createValidator)
    {
        _rbac = rbac;
        _createValidator = createValidator;
    }

    [HttpGet]
    [HasPermission("role.view")]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> List(CancellationToken ct)
        => Ok(ApiResponse<List<RoleDto>>.Ok(await _rbac.ListRolesAsync(ct)));

    [HttpPost]
    [HasPermission("role.manage")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        await ValidateAsync(_createValidator, request, ct);
        return Ok(ApiResponse<RoleDto>.Ok(await _rbac.CreateRoleAsync(request, ct)));
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermission("role.manage")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdatePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest request, CancellationToken ct)
        => Ok(ApiResponse<RoleDto>.Ok(await _rbac.UpdateRolePermissionsAsync(id, request, ct)));

    [HttpGet("{id:guid}/permissions")]
    [HasPermission("role.view")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetPermissions(Guid id, CancellationToken ct)
        => Ok(ApiResponse<List<string>>.Ok(await _rbac.GetRolePermissionsAsync(id, ct)));

    [HttpGet("/api/v1/permissions")]
    [HasPermission("role.view")]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> Permissions(CancellationToken ct)
        => Ok(ApiResponse<List<PermissionDto>>.Ok(await _rbac.ListPermissionsAsync(ct)));
}
