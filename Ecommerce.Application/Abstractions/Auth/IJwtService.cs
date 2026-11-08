using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Application.Abstractions.Auth;
public interface IJwtService
{
    TimeSpan RefreshTokenLifetime { get; }

    Result<string> GetAccessToken(string email, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);

    // 32 random bytes from the system's cryptographic generator, Base64Url encoded. Show it to the client once and store only its hash.
    string GenerateRefreshToken();

    // SHA-256 as lowercase hex. The token is already random, so a fast hash is enough (a password hash would only add cost).
    string HashRefreshToken(string refreshToken);
}
