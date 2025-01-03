using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;

namespace Ecommerce.Application.Users.Login;
internal sealed class LoginUserQueryHandler : IQueryHandler<LoginUserQuery, AccessTokenResponse>
{

    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    public LoginUserQueryHandler(IUserRepository userRepository, IAuthService authService, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _authService = authService;
        _jwtService = jwtService;
    }

    public async Task<Result<AccessTokenResponse>> Handle(
        LoginUserQuery request,
        CancellationToken cancellationToken)
    {

        User? user = await _userRepository.GetByEmail(request.Email);

        if (user is null)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.NotFound);
        }

        string hashedPassword = _authService.HashPassword(request.Password, user.PasswordSalt);

        if (hashedPassword != user.PasswordHash)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }


        Result<string> result = _jwtService.GetAccessToken(
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
