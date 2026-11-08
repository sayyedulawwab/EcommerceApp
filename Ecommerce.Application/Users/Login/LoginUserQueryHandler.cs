using System.Text;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Users.Login;
internal sealed class LoginUserQueryHandler(
    IApplicationDbContext dbContext,
    IAuthService authService,
    IJwtService jwtService,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<LoginUserQuery, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> Handle(
        LoginUserQuery request,
        CancellationToken cancellationToken)
    {
        Result<Email> email = Email.Create(request.Email);

        // A password bcrypt would cut short can't be one we stored, and checking only its first 72 bytes would accept a wrong one.
        if (email.IsFailure || Encoding.UTF8.GetByteCount(request.Password) > IAuthService.MaxPasswordBytes)
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidCredentials);
        }

        User? user = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == email.Value, cancellationToken);

        if (user is null)
        {
            authService.VerifyAgainstDummyHash(request.Password);

            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidCredentials);
        }

        if (!authService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidCredentials);
        }

        if (authService.NeedsRehash(user.PasswordHash))
        {
            user.UpdatePasswordHash(authService.HashPassword(request.Password));
        }

        Result<string> accessToken = jwtService.GetAccessToken(
            request.Email,
            user.Id.Value,
            user.IsAdmin,
            cancellationToken);

        if (accessToken.IsFailure)
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidCredentials);
        }

        DateTime now = dateTimeProvider.UtcNow;
        string refreshToken = jwtService.GenerateRefreshToken();

        dbContext.RefreshTokens.Add(RefreshToken.Create(
            user.Id,
            jwtService.HashRefreshToken(refreshToken),
            now,
            now.Add(jwtService.RefreshTokenLifetime)));

        // Saves the refresh token and, when the work factor changed, the new password hash.
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensResponse(accessToken.Value, refreshToken);
    }
}
