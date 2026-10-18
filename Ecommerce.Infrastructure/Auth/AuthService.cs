using System.Security.Cryptography;
using System.Text;
using Ecommerce.Application.Abstractions.Auth;

namespace Ecommerce.Infrastructure.Auth;
internal sealed class AuthService : IAuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 64;
    private const int Iterations = 210_000;

    public string GenerateSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSize));
    }

    public string HashPassword(string password, string salt)
    {
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            Convert.FromBase64String(salt),
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string salt, string passwordHash)
    {
        return FixedTimeEquals(HashPassword(password, salt), passwordHash)
            || FixedTimeEquals(HashLegacyPassword(password, salt), passwordHash);
    }

    // Accounts registered before the switch to PBKDF2 store SHA-256(password + salt).
    private static string HashLegacyPassword(string password, string salt)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
    }
}
