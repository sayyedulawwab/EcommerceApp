using Ecommerce.Application.Abstractions.Auth;
using Microsoft.Extensions.Options;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Ecommerce.Infrastructure.Auth;
internal sealed class AuthService(IOptions<PasswordHashingOptions> options) : IAuthService
{
    private readonly string _dummyHash = BCryptNet.HashPassword("dummy-password-for-timing", options.Value.WorkFactor);

    public string HashPassword(string password)
    {
        return BCryptNet.HashPassword(password, options.Value.WorkFactor);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCryptNet.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // The stored value is not a bcrypt hash (for example a row from before bcrypt). It can never match.
            return false;
        }
    }

    public bool NeedsRehash(string passwordHash)
    {
        return BCryptNet.PasswordNeedsRehash(passwordHash, options.Value.WorkFactor);
    }

    public void VerifyAgainstDummyHash(string password)
    {
        BCryptNet.Verify(password, _dummyHash);
    }
}
