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
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ecommerce.API.Controllers.Auth;
[Route("api/v{v:apiVersion}/auth")]
[ApiController]
public class AuthController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost("register")]
    [EnableRateLimiting(CredentialsRateLimitOptions.PolicyName)]
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

        Result<Guid> result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

    [MapToApiVersion(1)]
    [HttpPost("login")]
    [EnableRateLimiting(CredentialsRateLimitOptions.PolicyName)]
    public async Task<IActionResult> Login(
        LoginUserRequest request,
        ICommandHandler<LoginUserCommand, AuthTokensResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand(request.Email, request.Password);

        Result<AuthTokensResponse> result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        // The tokens go only into HttpOnly cookies, never into the body, so page scripts never see them.
        AuthCookies.Append(Response, result.Value);

        return NoContent();
    }

    [MapToApiVersion(1)]
    [HttpPost("refresh")]
    [EnableRateLimiting(TokensRateLimitOptions.PolicyName)]
    public async Task<IActionResult> Refresh(
        ICommandHandler<RefreshAccessTokenCommand, AuthTokensResponse> handler,
        CancellationToken cancellationToken)
    {
        string? refreshToken = Request.Cookies[AuthCookieNames.RefreshToken];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return UserErrors.InvalidRefreshToken.ToActionResult();
        }

        var command = new RefreshAccessTokenCommand(refreshToken);

        Result<AuthTokensResponse> result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            // The token is useless now (unknown, expired or revoked), so the browser drops both cookies.
            AuthCookies.Delete(Response);

            return result.Error.ToActionResult();
        }

        AuthCookies.Append(Response, result.Value);

        return NoContent();
    }

    [MapToApiVersion(1)]
    [HttpPost("logout")]
    [EnableRateLimiting(TokensRateLimitOptions.PolicyName)]
    public async Task<IActionResult> Logout(
        ICommandHandler<LogoutUserCommand> handler,
        CancellationToken cancellationToken)
    {
        string? refreshToken = Request.Cookies[AuthCookieNames.RefreshToken];

        // The cookies are cleared even when there is no token to revoke or the request fails.
        AuthCookies.Delete(Response);

        if (string.IsNullOrEmpty(refreshToken))
        {
            return NoContent();
        }

        var command = new LogoutUserCommand(refreshToken);

        Result result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return NoContent();
    }
}
