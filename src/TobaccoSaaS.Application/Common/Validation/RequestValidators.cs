using FluentValidation;
using TobaccoSaaS.Application.Features.Auth;
using TobaccoSaaS.Application.Features.Organization;
using TobaccoSaaS.Application.Features.Rbac;
using TobaccoSaaS.Application.Features.Tenancy;
using TobaccoSaaS.Domain.Common;

namespace TobaccoSaaS.Application.Common.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public sealed class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase alphanumeric words separated by hyphens.");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.AdminFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PlanCode).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100)
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Code may contain letters, digits and underscores only.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.PermissionCodes).NotNull();
    }
}

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).MinimumLength(8).When(x => !string.IsNullOrWhiteSpace(x.Password));
    }
}

public sealed class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequest>
{
    public AssignRoleRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public sealed class SetUserScopeRequestValidator : AbstractValidator<SetUserScopeRequest>
{
    public SetUserScopeRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ScopeLevel).NotEmpty().MaximumLength(30);
    }
}

public sealed class CreateOrgUnitRequestValidator : AbstractValidator<CreateOrgUnitRequest>
{
    public CreateOrgUnitRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.UnitType).NotEmpty().Must(IsValidUnitType)
            .WithMessage("UnitType must be one of: " + string.Join(", ", SpecVocabulary.UnitTypes));
        RuleFor(x => x.ParentId).NotEmpty().When(x => x.ParentId.HasValue);
    }

    private static bool IsValidUnitType(string? value)
        => SpecVocabulary.UnitTypes.Any(t => string.Equals(t, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public sealed class UpdateOrgUnitRequestValidator : AbstractValidator<UpdateOrgUnitRequest>
{
    public UpdateOrgUnitRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.UnitType).NotEmpty().Must(IsValidUnitType)
            .WithMessage("UnitType must be one of: " + string.Join(", ", SpecVocabulary.UnitTypes));
        RuleFor(x => x.ParentId).NotEmpty().When(x => x.ParentId.HasValue);
    }

    private static bool IsValidUnitType(string? value)
        => SpecVocabulary.UnitTypes.Any(t => string.Equals(t, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public sealed class CreatePositionRequestValidator : AbstractValidator<CreatePositionRequest>
{
    public CreatePositionRequestValidator()
    {
        RuleFor(x => x.OrgUnitId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReportsTo).NotEmpty().When(x => x.ReportsTo.HasValue);
        RuleFor(x => x.DefaultRoleId).NotEmpty().When(x => x.DefaultRoleId.HasValue);
    }
}

public sealed class UpdatePositionRequestValidator : AbstractValidator<UpdatePositionRequest>
{
    public UpdatePositionRequestValidator()
    {
        RuleFor(x => x.OrgUnitId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReportsTo).NotEmpty().When(x => x.ReportsTo.HasValue);
        RuleFor(x => x.DefaultRoleId).NotEmpty().When(x => x.DefaultRoleId.HasValue);
    }
}

public sealed class AssignEmployeePositionRequestValidator : AbstractValidator<AssignEmployeePositionRequest>
{
    public AssignEmployeePositionRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ValidFrom).NotEmpty();
        RuleFor(x => x.ValidTo).GreaterThanOrEqualTo(x => x.ValidFrom).When(x => x.ValidTo.HasValue);
    }
}
