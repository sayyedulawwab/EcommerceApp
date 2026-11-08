using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Users;

// Only the SHA-256 hash of the token is stored. The token itself is shown to the client once.
// Revoking happens in the database with a conditional update, so two requests can't both revoke the same token.
public sealed class RefreshToken : Entity<RefreshTokenId>
{
    private RefreshToken(RefreshTokenId id, UserId userId, string tokenHash, DateTime createdOnUtc, DateTime expiresOnUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedOnUtc = createdOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }

    public UserId UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }

    public bool IsRevoked => RevokedOnUtc is not null;

    public static RefreshToken Create(UserId userId, string tokenHash, DateTime createdOnUtc, DateTime expiresOnUtc)
    {
        return new RefreshToken(RefreshTokenId.New(), userId, tokenHash, createdOnUtc, expiresOnUtc);
    }

    // Handlers revoke with a conditional update instead, so this is for code that already holds a tracked token.
    public void Revoke(DateTime utcNow)
    {
        RevokedOnUtc ??= utcNow;
    }

    public bool IsExpired(DateTime utcNow)
    {
        return ExpiresOnUtc <= utcNow;
    }
}
