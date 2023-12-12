using Ecommerce.Application.Services.Auth.Commands;
using Ecommerce.Application.Services.Auth.Common;
using Ecommerce.Application.Services.Auth.Queries;
using Ecommerce.Contracts.Auth;
using Ecommerce.Domain.Common.Errors;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Contollers
{
    [Route("auth")]
    public class AuthController : ApiController
    {
        private readonly IAuthCommandService _authCommandService;
        private readonly IAuthQueryService _authQueryService;

        public AuthController(IAuthCommandService authCommandService, IAuthQueryService authQueryService)
        {
            _authCommandService = authCommandService;
            _authQueryService = authQueryService;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            ErrorOr<AuthResult> authResult = _authCommandService.Register(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password
                );

            if (authResult.IsError && authResult.FirstError == Errors.User.DuplicateEmail)
            {
                return Problem
                    (
                        statusCode: StatusCodes.Status409Conflict,
                        title: authResult.FirstError.Description
                    );
            }
            return authResult.Match
                (
                    authResult => Ok(MapAuthResult(authResult)),
                    errors => Problem(errors)
                );

           
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            ErrorOr<AuthResult> authResult = _authQueryService.Login(
                request.Email,
                request.Password);

            if (authResult.IsError && authResult.FirstError == Errors.Auth.InvalidCredentials)
            {
                return Problem
                    (
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: authResult.FirstError.Description
                    );
            }
            return authResult.Match
               (
                   authResult => Ok(MapAuthResult(authResult)),
                   errors => Problem(errors)
               );

        }

        private static AuthResponse MapAuthResult(AuthResult authResult)
        {
            return new AuthResponse(authResult.user.Id,
                                    authResult.user.FirstName,
                                    authResult.user.LastName,
                                    authResult.user.Email,
                                    authResult.token);
        }
    }
}
