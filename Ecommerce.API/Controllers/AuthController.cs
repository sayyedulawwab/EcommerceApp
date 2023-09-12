using Ecommerce.Models.APIModels;
using Ecommerce.Services.Abstractions.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDTO model)
        {
            var registrationSuccessful = _authService.Register(model);

            if (!registrationSuccessful)
            {
                return BadRequest("Registration failed. Email might be in use.");
            }

            
            return GenerateToken(model.Email);
        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDTO model)
        {
            if (string.IsNullOrEmpty(model.Username))
            {
                return BadRequest("Username must be provided");
            }

            var user = _authService.Login(model);

            if (user == null)
            {
                return Unauthorized();
            }

            return GenerateToken(user.Email);
        }

        private IActionResult GenerateToken(string email)
        {
            const string secretKey = "ThisIsASecretKey"; 
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var jwt = new JwtSecurityToken(
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
                claims: new[] { new Claim(ClaimTypes.Email, email) }
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return Ok(new { token });
        }
    }

    
}
