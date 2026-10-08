using System.Text.Json;
using TobaccoSaaS.Application.Common.Exceptions;
using TobaccoSaaS.Application.Common.Models;

namespace TobaccoSaaS.Api.Middleware;

/// <summary>
/// Single exception boundary (AGENTS.md §19). Maps expected application exceptions to the
/// standard envelope and turns everything else into a safe 500 — no stack traces, SQL or
/// paths ever reach the client (spec §20.1).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationAppException ex)
        {
            _logger.LogInformation("Validation failed on {Path}.", context.Request.Path);
            await WriteAsync(context, ex.StatusCode, ApiResponse<object>.Fail(ex.Code, ex.Message, ex.Errors));
        }
        catch (AppException ex)
        {
            _logger.LogInformation("Application error {Code} on {Path}.", ex.Code, context.Request.Path);
            await WriteAsync(context, ex.StatusCode, ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}.", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("server_error", "An unexpected error occurred. Please try again."));
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, ApiResponse<object> body)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
