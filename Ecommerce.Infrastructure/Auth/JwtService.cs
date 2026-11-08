using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
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
    private const int RefreshTokenBytes = 32;

    private readonly JsonWebTokenHandler _tokenHandler = new();

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(jwtOptions.Value.RefreshTokenLifetimeDays);

    public string GenerateRefreshToken()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
    }

    public string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }

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
