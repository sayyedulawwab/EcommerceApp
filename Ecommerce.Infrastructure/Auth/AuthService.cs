using System.Security.Cryptography;
using System.Text;
using Ecommerce.Application.Abstractions.Auth;

namespace Ecommerce.Infrastructure.Auth;
internal sealed class AuthService : IAuthService
{
    public string GenerateSalt()
    {
        byte[] randomBytes = new byte[128 / 8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashPassword(string password, string salt)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
    }
}
