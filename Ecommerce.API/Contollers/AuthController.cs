using Ecommerce.Application.Auth.Commands.Register;
using Ecommerce.Application.Auth.Common;
using Ecommerce.Application.Auth.Queries.Login;
using Ecommerce.Contracts.Auth;
using Ecommerce.Domain.Common.Errors;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Contollers
{
    [Route("auth")]
    public class AuthController : ApiController
    {
        private readonly ISender _mediator;

        public AuthController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var command = new RegisterCommand(request.FirstName,
                request.LastName,
                request.Email,
                request.Password);

            ErrorOr<AuthResult> authResult = await _mediator.Send(command);

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
        public async Task<IActionResult> Login(LoginRequest request)
        {

            var query = new LoginQuery(request.Email, request.Password);

            ErrorOr<AuthResult> authResult = await _mediator.Send(query);

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
