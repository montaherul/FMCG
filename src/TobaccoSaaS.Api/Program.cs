using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TobaccoSaaS.Api.Middleware;
using TobaccoSaaS.Api.Security;
using TobaccoSaaS.Application;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Application.Common.Models;
using TobaccoSaaS.Infrastructure;
using TobaccoSaaS.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

LoadDotEnvFile();
builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext());

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kv => kv.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var payload = ApiResponse<object>.Fail("validation_error", "One or more validation errors occurred.", errors);
        return new BadRequestObjectResult(payload);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Tobacco SaaS API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, HttpRequestContext>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options => options.AddPolicy("spa", policy =>
    policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await SeedAsync(app);

app.Run();

static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    try
    {
        await scope.ServiceProvider.GetRequiredService<IDataSeeder>().SeedAsync();
    }
    catch (Exception ex)
    {
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>()
            .LogWarning(ex, "Database seeding skipped (database unavailable). The API will still start.");
    }
}

static void LoadDotEnvFile()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, ".env");
        if (File.Exists(candidate))
        {
            foreach (var rawLine in File.ReadAllLines(candidate))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim().Trim('"');
                if (key.Length == 0)
                {
                    continue;
                }

                if (Environment.GetEnvironmentVariable(key) is null)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return;
        }

        directory = directory.Parent;
    }
}

public partial class Program;
