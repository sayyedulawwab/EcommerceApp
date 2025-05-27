using Asp.Versioning;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Users.Login;
using Ecommerce.Application.Users.Register;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Auth;
[Route("api/v{v:apiVersion}/auth")]
[ApiController]
public class AuthController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterUserRequest request,
        ICommandHandler<RegisterUserCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [MapToApiVersion(1)]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginUserRequest request,
        IQueryHandler<LoginUserQuery, AccessTokenResponse> handler,
        CancellationToken cancellationToken)
    {
        var query = new LoginUserQuery(request.Email, request.Password);

        Result<AccessTokenResponse> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
