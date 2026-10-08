namespace TobaccoSaaS.Application.Features.Auth;

public sealed record LoginRequest(string Email, string Password, string? DeviceLabel = null);

public sealed record RefreshRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record UserProfileDto(
    Guid Id,
    Guid? TenantId,
    string Email,
    string FullName,
    bool IsPlatformScope,
    Guid? ScopeNodeId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserProfileDto User);
