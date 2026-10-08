using System.Text.Json;
using TobaccoSaaS.Application.Common.Exceptions;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Domain.Entities.Audit;
using TobaccoSaaS.Domain.Entities.Identity;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Application.Features.Auth;

public sealed class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutStep = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromHours(12);

    private readonly IIdentityRepository _identity;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IAuditWriter _audit;
    private readonly IEmailSender _emailSender;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AuthService(
        IIdentityRepository identity,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAuditWriter audit,
        IEmailSender emailSender,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _identity = identity;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _audit = audit;
        _emailSender = emailSender;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var now = _clock.UtcNow;

        var user = await _identity.GetUserForAuthByEmailAsync(email, ct);

        if (user is null)
        {
            await WriteAuditAsync(null, null, "auth.login.failed", "Identity",
                "Invalid credentials (unknown email).", ct);
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        if (user.Status is UserStatus.Terminated or UserStatus.Inactive or UserStatus.Suspended)
        {
            await WriteAuditAsync(user.TenantId, user.Id, "auth.login.denied", "Identity",
                $"Account status {user.Status}.", ct);
            throw new ForbiddenException("This account is not active. Please contact your administrator.");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
        {
            throw new ForbiddenException($"Account is temporarily locked. Try again after {user.LockoutEnd.Value:u}.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                var steps = user.FailedLoginCount / MaxFailedAttempts;
                user.LockoutEnd = now.Add(LockoutStep * steps);
            }

            await _identity.SaveChangesAsync(ct);
            await WriteAuditAsync(user.TenantId, user.Id, "auth.login.failed", "Identity",
                $"Failed attempt #{user.FailedLoginCount}.", ct);
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        if (user.IsMfaEnabled)
        {
            // MFA data model is ready (spec §4.1); interactive TOTP verification ships with the
            // auth hardening pass. Refuse rather than silently downgrade security.
            throw new BusinessRuleException("MFA is enabled for this account but verification is not yet available.", "mfa_not_available");
        }

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = now;

        var profile = await BuildProfileAsync(user, ct);
        var (refreshToken, refreshHash) = _tokenService.CreateRefreshToken();

        await _identity.AddSessionAsync(new UserSession
        {
            UserId = user.Id,
            RefreshTokenHash = refreshHash,
            ExpiresAt = now.Add(RefreshLifetime),
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            DeviceLabel = request.DeviceLabel
        }, ct);

        await _identity.SaveChangesAsync(ct);
        await WriteAuditAsync(user.TenantId, user.Id, "auth.login", "Identity", null, ct);

        return Issue(profile, refreshToken, now);
    }

    public async Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var hash = _tokenService.HashToken(request.RefreshToken);

        var session = await _identity.GetSessionByHashAsync(hash, ct);
        if (session is null || session.RevokedAt.HasValue || session.ExpiresAt <= now)
        {
            throw new UnauthorizedAppException("The refresh token is invalid or has expired.");
        }

        var user = await _identity.GetUserForAuthByIdAsync(session.UserId, ct);
        if (user is null || user.Status is UserStatus.Terminated or UserStatus.Inactive or UserStatus.Suspended)
        {
            throw new UnauthorizedAppException("The refresh token is invalid or has expired.");
        }

        var (newToken, newHash) = _tokenService.CreateRefreshToken();
        session.RevokedAt = now;
        session.ReplacedByHash = newHash;

        await _identity.AddSessionAsync(new UserSession
        {
            UserId = user.Id,
            RefreshTokenHash = newHash,
            ExpiresAt = now.Add(RefreshLifetime),
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent
        }, ct);

        await _identity.SaveChangesAsync(ct);

        var profile = await BuildProfileAsync(user, ct);
        return Issue(profile, newToken, now);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = _tokenService.HashToken(refreshToken);
        var session = await _identity.GetSessionByHashAsync(hash, ct);
        if (session is null || session.RevokedAt.HasValue)
        {
            return;
        }

        session.RevokedAt = _clock.UtcNow;
        await _identity.SaveChangesAsync(ct);
        await WriteAuditAsync(null, session.UserId, "auth.logout", "Identity", null, ct);
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _identity.GetUserByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");
        return await BuildProfileAsync(user, ct);
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var user = await _identity.GetUserForAuthByEmailAsync(email, ct);

        // Never reveal whether the email exists (spec §4.1).
        if (user is null || user.Status is UserStatus.Terminated)
        {
            return;
        }

        var (token, hash) = _tokenService.CreateRefreshToken();
        user.ResetTokenHash = hash;
        user.ResetTokenExpiresAt = _clock.UtcNow.AddHours(1);
        await _identity.SaveChangesAsync(ct);

        await _emailSender.SendAsync(user.Email,
            "Reset your password",
            $"Use this single-use token within 1 hour to reset your password: {token}", ct);

        await WriteAuditAsync(user.TenantId, user.Id, "auth.password.reset.requested", "Identity", null, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var user = await _identity.GetUserForAuthByEmailAsync(email, ct);

        if (user is null
            || string.IsNullOrEmpty(user.ResetTokenHash)
            || user.ResetTokenExpiresAt is null
            || user.ResetTokenExpiresAt <= _clock.UtcNow
            || !string.Equals(user.ResetTokenHash, _tokenService.HashToken(request.Token), StringComparison.Ordinal))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["token"] = new[] { "The reset token is invalid or has expired." }
            });
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordChangedAt = _clock.UtcNow;
        user.ResetTokenHash = null;
        user.ResetTokenExpiresAt = null;
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;

        foreach (var session in user.Sessions.Where(s => s.RevokedAt is null))
        {
            session.RevokedAt = _clock.UtcNow;
        }

        await _identity.SaveChangesAsync(ct);
        await WriteAuditAsync(user.TenantId, user.Id, "auth.password.reset", "Identity", null, ct);
    }

    private async Task<UserProfileDto> BuildProfileAsync(User user, CancellationToken ct)
    {
        var roles = await _identity.GetRoleCodesAsync(user.Id, ct);
        var permissions = await _identity.GetPermissionCodesAsync(user.Id, ct);
        var isPlatform = user.TenantId is null;
        var scopeNodeId = user.Scopes.FirstOrDefault(s => s.IsActive)?.ScopeNodeId;

        return new UserProfileDto(user.Id, user.TenantId, user.Email, user.FullName,
            isPlatform, scopeNodeId, roles, permissions);
    }

    private TokenResponse Issue(UserProfileDto profile, string refreshToken, DateTimeOffset now)
    {
        var user = new User { Id = profile.Id, Email = profile.Email, TenantId = profile.TenantId };
        var access = _tokenService.CreateAccessToken(user, profile.Roles, profile.IsPlatformScope, profile.ScopeNodeId);

        return new TokenResponse(access.Token, access.ExpiresAt, refreshToken,
            now.Add(RefreshLifetime), profile);
    }

    private Task WriteAuditAsync(Guid? tenantId, Guid? userId, string action, string module, string? detail, CancellationToken ct)
    {
        return _audit.WriteAsync(new AuditLog
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            Module = module,
            NewValues = detail is null ? null : JsonSerializer.Serialize(new { detail }),
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent
        }, ct);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
