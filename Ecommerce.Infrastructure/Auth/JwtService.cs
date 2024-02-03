using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Domain.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ecommerce.Infrastructure.Auth;
internal sealed class JwtService : IJwtService
{
    private readonly AuthenticationOptions _authOptions;
    public JwtService(IOptions<AuthenticationOptions> authOptions)
    {

        _authOptions = authOptions.Value;

    }
    public Result<string> GetAccessToken(string email, Guid userId, CancellationToken cancellationToken = default)
    {
        string secretKey = _authOptions.SecretKey;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var jwt = new JwtSecurityToken(
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            claims: new[] {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);


        return Result.Success(token);

    }
    
}
