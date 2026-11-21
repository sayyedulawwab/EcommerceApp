namespace Ecommerce.Application.Abstractions.Auth;
public interface IAuthService
{
    // bcrypt only uses the first 72 bytes of a password, so longer ones are rejected instead of silently cut.
    const int MaxPasswordBytes = 72;

    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    bool NeedsRehash(string passwordHash);

    // Does the same work as VerifyPassword against a throwaway hash, so an unknown email takes as long as a wrong password.
    void VerifyAgainstDummyHash(string password);
}
