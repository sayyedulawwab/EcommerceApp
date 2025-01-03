using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Domain.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure.Auth;
internal sealed class JwtService : IJwtService
{
    private readonly JwtOptions _jwtOptions;
    public JwtService(IOptions<JwtOptions> jwtOptions)
    {

        _jwtOptions = jwtOptions.Value;

    }
    public Result<string> GetAccessToken(string email, Guid userId, CancellationToken cancellationToken = default)
    {

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims = [
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ];

        var jwt = new JwtSecurityToken(
                    _jwtOptions.Issuer,
                    _jwtOptions.Audience,
                    claims,
                    null,
                    DateTime.UtcNow.AddHours(1),
                    signingCredentials);

        string token = new JwtSecurityTokenHandler().WriteToken(jwt);


        return Result.Success(token);

    }

}
