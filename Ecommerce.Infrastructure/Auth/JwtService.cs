using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Domain.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure.Auth;
internal sealed class JwtService(IOptions<JwtOptions> jwtOptions) : IJwtService
{
    public Result<string> GetAccessToken(string email, Guid userId, CancellationToken cancellationToken = default)
    {

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.SecretKey));

        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims = [
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ];

        var jwt = new JwtSecurityToken(
                    jwtOptions.Value.Issuer,
                    jwtOptions.Value.Audience,
                    claims,
                    null,
                    DateTime.UtcNow.AddHours(1),
                    signingCredentials);

        string token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return Result.Success(token);

    }

}
