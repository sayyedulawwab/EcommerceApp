using Ecommerce.Application.Auth.Commands.Register;
using Ecommerce.Application.Auth.Common;
using Ecommerce.Application.Auth.Queries.Login;
using Ecommerce.Contracts.Auth;
using Ecommerce.Domain.Common.Errors;
using ErrorOr;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Contollers
{
    [Route("auth")]
    public class AuthController : ApiController
    {
        private readonly ISender _mediator;
        private readonly IMapper _mapper;

        public AuthController(ISender mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var command = _mapper.Map<RegisterCommand>(request);

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
                    authResult => Ok(_mapper.Map<AuthResponse>(authResult)),
                    errors => Problem(errors)
                );

           
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {

            var query = _mapper.Map<LoginQuery>(request);

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
                   authResult => Ok(_mapper.Map<AuthResponse>(authResult)),
                   errors => Problem(errors)
               );

        }

     
    }
}
