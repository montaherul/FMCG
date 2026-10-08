using TobaccoSaaS.Domain.Entities.Identity;

namespace TobaccoSaaS.Application.Common.Interfaces;

/// <summary>Password hashing abstraction (bcrypt/argon2id — spec §21.1).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

/// <summary>JWT access-token issuance and opaque refresh/reset token helpers.</summary>
public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user, IEnumerable<string> roles, bool isPlatform, Guid? scopeNodeId);

    /// <summary>Returns the raw token (given to the client) and its stored hash.</summary>
    (string Token, string Hash) CreateRefreshToken();

    string HashToken(string token);
}

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
