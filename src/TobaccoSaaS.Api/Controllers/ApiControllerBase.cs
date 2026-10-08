using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TobaccoSaaS.Application.Common.Exceptions;

namespace TobaccoSaaS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Runs the request validator and surfaces failures through the standard envelope.</summary>
    protected async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => JsonNamingPolicy.CamelCase.ConvertName(g.Key),
                g => g.Select(e => e.ErrorMessage).ToArray());

        throw new ValidationAppException(errors);
    }
}
