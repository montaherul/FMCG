namespace TobaccoSaaS.Application.Common.Models;

/// <summary>Standard error payload. Never carries stack traces or internal details (AGENTS.md §19).</summary>
public sealed record ApiError(string Code, string Message, object? Details = null);

/// <summary>Uniform response envelope used by every endpoint (spec §20.1).</summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public ApiError? Error { get; init; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };

    public static ApiResponse<T> Fail(string code, string message, object? details = null) =>
        new() { Success = false, Error = new ApiError(code, message, details) };
}

/// <summary>Server-side pagination envelope (spec §20.1).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
