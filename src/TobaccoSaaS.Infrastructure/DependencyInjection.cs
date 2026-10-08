using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TobaccoSaaS.Application.Common;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Infrastructure.Data;
using TobaccoSaaS.Infrastructure.Data.Seed;
using TobaccoSaaS.Infrastructure.Email;
using TobaccoSaaS.Infrastructure.Repositories;
using TobaccoSaaS.Infrastructure.Security;

namespace TobaccoSaaS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                // Required join navigations (user_roles -> roles/users, role_permissions -> roles)
                // are only ever traversed from the scoped side and never rely on the filtered
                // principal being loaded, so the filter/required-navigation interaction is safe here.
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<IDataSeeder, DataSeeder>();

        return services;
    }
}
