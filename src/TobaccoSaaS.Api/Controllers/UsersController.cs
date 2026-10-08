using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TobaccoSaaS.Api.Security;
using TobaccoSaaS.Application.Common.Models;
using TobaccoSaaS.Application.Features.Rbac;

namespace TobaccoSaaS.Api.Controllers;

/// <summary>Tenant-scoped user administration: listing, creation, role assignment and scope (spec §6).</summary>
public sealed class UsersController : ApiControllerBase
{
    private readonly IRbacService _rbac;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<AssignRoleRequest> _assignValidator;
    private readonly IValidator<SetUserScopeRequest> _scopeValidator;

    public UsersController(
        IRbacService rbac,
        IValidator<CreateUserRequest> createValidator,
        IValidator<AssignRoleRequest> assignValidator,
        IValidator<SetUserScopeRequest> scopeValidator)
    {
        _rbac = rbac;
        _createValidator = createValidator;
        _assignValidator = assignValidator;
        _scopeValidator = scopeValidator;
    }

    [HttpGet]
    [HasPermission("user.view")]
    public async Task<ActionResult<ApiResponse<List<UserSummaryDto>>>> List(CancellationToken ct)
        => Ok(ApiResponse<List<UserSummaryDto>>.Ok(await _rbac.ListUsersAsync(ct)));

    [HttpPost]
    [HasPermission("user.create")]
    public async Task<ActionResult<ApiResponse<CreatedUserResult>>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        await ValidateAsync(_createValidator, request, ct);
        return Ok(ApiResponse<CreatedUserResult>.Ok(await _rbac.CreateUserAsync(request, ct)));
    }

    [HttpPost("{id:guid}/roles")]
    [HasPermission("user.roles")]
    public async Task<ActionResult<ApiResponse<object>>> AssignRole(Guid id, [FromBody] AssignRoleBody body, CancellationToken ct)
    {
        var request = new AssignRoleRequest(id, body.RoleId);
        await ValidateAsync(_assignValidator, request, ct);
        await _rbac.AssignRoleAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPut("{id:guid}/scope")]
    [HasPermission("user.scope")]
    public async Task<ActionResult<ApiResponse<object>>> SetScope(Guid id, [FromBody] SetScopeBody body, CancellationToken ct)
    {
        var request = new SetUserScopeRequest(id, body.ScopeNodeId, body.ScopeLevel);
        await ValidateAsync(_scopeValidator, request, ct);
        await _rbac.SetUserScopeAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    public sealed record AssignRoleBody(Guid RoleId);

    public sealed record SetScopeBody(Guid? ScopeNodeId, string ScopeLevel);
}
