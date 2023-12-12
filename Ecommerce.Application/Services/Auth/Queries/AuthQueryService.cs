using Ecommerce.Application.Common.Interfaces.Auth;
using Ecommerce.Application.Common.Interfaces.Persistence;
using Ecommerce.Application.Services.Auth.Common;
using Ecommerce.Domain.Common.Errors;
using Ecommerce.Domain.Entities;
using ErrorOr;

namespace Ecommerce.Application.Services.Auth.Queries
{
    public class AuthQueryService : IAuthQueryService
    {
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IUserRepository _userRepository;


        public AuthQueryService(IJwtTokenGenerator jwtTokenGenerator, IUserRepository userRepository)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
            _userRepository = userRepository;
        }


        public ErrorOr<AuthResult> Login(string email, string password)
        {
            // 1. Validate the user exists
            if (_userRepository.GetUserByEmail(email) is not User user)
            {
                return Errors.Auth.InvalidCredentials;
            }

            // 2. password is correct

            if (user.Password != password)
            {
                return Errors.Auth.InvalidCredentials;
            }

            // 3. create JWT token
            var token = _jwtTokenGenerator.GenerateJwtToken(user);

            return new AuthResult(user, token);
        }


    }
}
