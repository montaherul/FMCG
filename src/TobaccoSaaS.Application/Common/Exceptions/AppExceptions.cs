namespace TobaccoSaaS.Application.Common.Exceptions;

/// <summary>Base class for expected application failures mapped to HTTP status codes centrally.</summary>
public abstract class AppException : Exception
{
    protected AppException(string code, string message, int statusCode) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }

    public int StatusCode { get; }
}

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(IDictionary<string, string[]> errors)
        : base("validation_error", "One or more validation errors occurred.", 400)
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message = "Resource not found.")
        : base("not_found", message, 404) { }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You are not permitted to perform this action.")
        : base("forbidden", message, 403) { }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Authentication failed.")
        : base("unauthorized", message, 401) { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base("conflict", message, 409) { }
}

public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, string code = "business_rule")
        : base(code, message, 422) { }
}
