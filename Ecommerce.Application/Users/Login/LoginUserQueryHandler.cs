using System.Text;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Users.Login;
internal sealed class LoginUserQueryHandler(
    IApplicationDbContext dbContext,
    IAuthService authService,
    IJwtService jwtService)
    : IQueryHandler<LoginUserQuery, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(
        LoginUserQuery request,
        CancellationToken cancellationToken)
    {
        Result<Email> email = Email.Create(request.Email);

        // A password bcrypt would cut short can't be one we stored, and checking only its first 72 bytes would accept a wrong one.
        if (email.IsFailure || Encoding.UTF8.GetByteCount(request.Password) > IAuthService.MaxPasswordBytes)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        User? user = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == email.Value, cancellationToken);

        if (user is null)
        {
            authService.VerifyAgainstDummyHash(request.Password);

            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        if (!authService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        if (authService.NeedsRehash(user.PasswordHash))
        {
            user.UpdatePasswordHash(authService.HashPassword(request.Password));

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        Result<string> result = jwtService.GetAccessToken(
            request.Email,
            user.Id.Value,
            user.IsAdmin,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        return new AccessTokenResponse(result.Value);
    }
}
