using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TobaccoSaaS.Application.Common.Exceptions;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Application.Common.Models;
using TobaccoSaaS.Application.Features.Auth;

namespace TobaccoSaaS.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ResetPasswordRequest> _resetValidator;

    public AuthController(
        IAuthService auth,
        ICurrentUserService currentUser,
        IValidator<LoginRequest> loginValidator,
        IValidator<ResetPasswordRequest> resetValidator)
    {
        _auth = auth;
        _currentUser = currentUser;
        _loginValidator = loginValidator;
        _resetValidator = resetValidator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        await ValidateAsync(_loginValidator, request, ct);
        return Ok(ApiResponse<TokenResponse>.Ok(await _auth.LoginAsync(request, ct)));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
        => Ok(ApiResponse<TokenResponse>.Ok(await _auth.RefreshAsync(request, ct)));

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        await _auth.LogoutAsync(request.RefreshToken, ct);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> Profile(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAppException();
        return Ok(ApiResponse<UserProfileDto>.Ok(await _auth.GetProfileAsync(userId, ct)));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _auth.RequestPasswordResetAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "If the email exists, a reset link has been sent." }));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await ValidateAsync(_resetValidator, request, ct);
        await _auth.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Password has been reset." }));
    }
}
