namespace TobaccoSaaS.Application.Features.Auth;

public interface IAuthService
{
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);

    Task LogoutAsync(string refreshToken, CancellationToken ct = default);

    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Always succeeds from the caller's perspective; never reveals whether the email exists.</summary>
    Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken ct = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
