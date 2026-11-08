using Asp.Versioning;
using Ecommerce.API.Extensions;
using Ecommerce.API.Infrastructure;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Users;
using Ecommerce.Application.Users.Login;
using Ecommerce.Application.Users.Logout;
using Ecommerce.Application.Users.Refresh;
using Ecommerce.Application.Users.Register;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ecommerce.API.Controllers.Auth;
[Route("api/v{v:apiVersion}/auth")]
[ApiController]
[EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
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

        return Ok(new { id = result.Value });
    }

    [MapToApiVersion(1)]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginUserRequest request,
        ICommandHandler<LoginUserCommand, AuthTokensResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand(request.Email, request.Password);

        Result<AuthTokensResponse> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [MapToApiVersion(1)]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request,
        ICommandHandler<RefreshAccessTokenCommand, AuthTokensResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new RefreshAccessTokenCommand(request.RefreshToken);

        Result<AuthTokensResponse> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [MapToApiVersion(1)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        ICommandHandler<LogoutUserCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new LogoutUserCommand(request.RefreshToken);

        Result result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return NoContent();
    }
}
