using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure.Auth;
internal sealed class JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options)
    {
        // Tokens are validated by JsonWebTokenHandler (the default). The old JwtSecurityTokenHandler
        // stays off, and the claims keep the names JwtService wrote (ClaimTypes.NameIdentifier, Email, Role).
        options.UseSecurityTokenValidators = false;

        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidIssuer = jwtOptions.Value.Issuer,
            ValidAudience = jwtOptions.Value.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Value.SecretKey)),
            // The default tolerance is 5 minutes, which would stretch a 15-minute token to 20.
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        Configure(options);
    }
}
