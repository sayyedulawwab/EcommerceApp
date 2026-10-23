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

        User? user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email == new Email(request.Email), cancellationToken);

        if (user is null || !authService.VerifyPassword(request.Password, user.PasswordSalt, user.PasswordHash))
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        Result<string> result = jwtService.GetAccessToken(
            request.Email,
            user.Id.Value,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        return new AccessTokenResponse(result.Value);
    }
}
