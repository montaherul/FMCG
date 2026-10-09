using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TobaccoSaaS.Application.Common.Validation;
using TobaccoSaaS.Application.Features.Auth;
using TobaccoSaaS.Application.Features.Organization;
using TobaccoSaaS.Application.Features.Rbac;
using TobaccoSaaS.Application.Features.Tenancy;

namespace TobaccoSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IRbacService, RbacService>();
        services.AddScoped<IOrganizationService, OrganizationService>();

        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordRequestValidator>();
        services.AddScoped<IValidator<CreateTenantRequest>, CreateTenantRequestValidator>();
        services.AddScoped<IValidator<CreateRoleRequest>, CreateRoleRequestValidator>();
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<AssignRoleRequest>, AssignRoleRequestValidator>();
        services.AddScoped<IValidator<SetUserScopeRequest>, SetUserScopeRequestValidator>();
        services.AddScoped<IValidator<CreateOrgUnitRequest>, CreateOrgUnitRequestValidator>();
        services.AddScoped<IValidator<UpdateOrgUnitRequest>, UpdateOrgUnitRequestValidator>();
        services.AddScoped<IValidator<CreatePositionRequest>, CreatePositionRequestValidator>();
        services.AddScoped<IValidator<UpdatePositionRequest>, UpdatePositionRequestValidator>();
        services.AddScoped<IValidator<AssignEmployeePositionRequest>, AssignEmployeePositionRequestValidator>();

        return services;
    }
}
