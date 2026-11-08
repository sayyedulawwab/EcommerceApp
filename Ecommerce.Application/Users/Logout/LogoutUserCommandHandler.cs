using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Users.Logout;
internal sealed class LogoutUserCommandHandler(
    IApplicationDbContext dbContext,
    IJwtService jwtService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<LogoutUserCommand>
{
    public async Task<Result> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        string tokenHash = jwtService.HashRefreshToken(request.RefreshToken);
        DateTime now = dateTimeProvider.UtcNow;

        // An unknown or already revoked token is not an error: the caller ends up signed out either way,
        // and the response does not tell anyone whether a token exists.
        await dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedOnUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedOnUtc, now), cancellationToken);

        return Result.Success();
    }
}
