using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;

namespace Ecommerce.Application.Users.Login;
internal sealed class LoginUserQueryHandler(
    IUserRepository userRepository,
    IAuthService authService,
    IJwtService jwtService)
    : IQueryHandler<LoginUserQuery, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(
        LoginUserQuery request,
        CancellationToken cancellationToken)
    {

        User? user = await userRepository.GetByEmail(request.Email);

        if (user is null)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.NotFound);
        }

        string hashedPassword = authService.HashPassword(request.Password, user.PasswordSalt);

        if (hashedPassword != user.PasswordHash)
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
