using System.Text;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Auth;
internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is required.");
        }

        // The message never includes the key itself.
        if (string.IsNullOrEmpty(options.SecretKey))
        {
            failures.Add("Jwt:SecretKey is required. Set it with user-secrets or the Jwt__SecretKey environment variable.");
        }
        else if (Encoding.UTF8.GetByteCount(options.SecretKey) < JwtOptions.MinSecretKeyBytes)
        {
            failures.Add($"Jwt:SecretKey must be at least {JwtOptions.MinSecretKeyBytes} bytes.");
        }

        if (options.AccessTokenLifetimeMinutes is < 1 or > 60)
        {
            failures.Add("Jwt:AccessTokenLifetimeMinutes must be between 1 and 60.");
        }

        if (options.RefreshTokenLifetimeDays is < 1 or > 90)
        {
            failures.Add("Jwt:RefreshTokenLifetimeDays must be between 1 and 90.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
