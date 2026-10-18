namespace Ecommerce.Application.Abstractions.Auth;
public interface IAuthService
{
    string GenerateSalt();
    string HashPassword(string password, string salt);
    bool VerifyPassword(string password, string salt, string passwordHash);
}
