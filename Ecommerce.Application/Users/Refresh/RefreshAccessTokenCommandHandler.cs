using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Users.Refresh;
internal sealed class RefreshAccessTokenCommandHandler(
    IApplicationDbContext dbContext,
    IJwtService jwtService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RefreshAccessTokenCommand, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        DateTime now = dateTimeProvider.UtcNow;
        string tokenHash = jwtService.HashRefreshToken(request.RefreshToken);

        RefreshToken? stored = await dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (stored is null)
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidRefreshToken);
        }

        // A token that was already rotated or logged out is being used again: someone may have copied it.
        // Cut off every session of that user, so the thief's newer token stops working too.
        if (stored.IsRevoked)
        {
            await RevokeAllAsync(stored.UserId, now, cancellationToken);

            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidRefreshToken);
        }

        if (stored.IsExpired(now))
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidRefreshToken);
        }

        // The conditional update lets only one of two concurrent requests with the same token win.
        int revoked = await dbContext.RefreshTokens
            .Where(token => token.Id == stored.Id && token.RevokedOnUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedOnUtc, now), cancellationToken);

        if (revoked == 0)
        {
            await RevokeAllAsync(stored.UserId, now, cancellationToken);

            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidRefreshToken);
        }

        User? user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == stored.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidRefreshToken);
        }

        Result<string> accessToken = jwtService.GetAccessToken(user.Email.Value, user.Id.Value, user.IsAdmin, cancellationToken);

        if (accessToken.IsFailure)
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidRefreshToken);
        }

        string refreshToken = jwtService.GenerateRefreshToken();

        dbContext.RefreshTokens.Add(RefreshToken.Create(
            user.Id,
            jwtService.HashRefreshToken(refreshToken),
            now,
            now.Add(jwtService.RefreshTokenLifetime)));

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensResponse(accessToken.Value, refreshToken);
    }

    private Task<int> RevokeAllAsync(UserId userId, DateTime now, CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedOnUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedOnUtc, now), cancellationToken);
    }
}
