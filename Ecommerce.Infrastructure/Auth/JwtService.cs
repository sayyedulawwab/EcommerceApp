using System.Security.Claims;
using System.Text;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Domain.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure.Auth;
internal sealed class JwtService(IOptions<JwtOptions> jwtOptions, IDateTimeProvider dateTimeProvider) : IJwtService
{
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public Result<string> GetAccessToken(string email, Guid userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.SecretKey));

        List<Claim> claims = [
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ];

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, Roles.Admin));
        }

        DateTime now = dateTimeProvider.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Value.Issuer,
            Audience = jwtOptions.Value.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(jwtOptions.Value.AccessTokenLifetimeMinutes),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        return Result.Success(_tokenHandler.CreateToken(descriptor));
    }
}
