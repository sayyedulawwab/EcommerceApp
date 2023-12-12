using Ecommerce.Application.Common.Interfaces.Auth;
using Ecommerce.Application.Common.Interfaces.Persistence;
using Ecommerce.Application.Services.Auth.Common;
using Ecommerce.Domain.Common.Errors;
using Ecommerce.Domain.Entities;
using ErrorOr;

namespace Ecommerce.Application.Services.Auth.Commands
{
    public class AuthCommandService : IAuthCommandService
    {
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IUserRepository _userRepository;


        public AuthCommandService(IJwtTokenGenerator jwtTokenGenerator, IUserRepository userRepository)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
            _userRepository = userRepository;
        }

        public ErrorOr<AuthResult> Register(string firstName, string lastName, string email, string password)
        {
            // 1. Validate the uesr doesn't exist
            if (_userRepository.GetUserByEmail(email) is not null)
            {
                return Errors.User.DuplicateEmail;
            }

            // 2. create user (generate unique id)
            var user = new User()
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Password = password
            };

            _userRepository.Add(user);


            // 3. generate Jwt token
            var token = _jwtTokenGenerator.GenerateJwtToken(user);

            return new AuthResult(user, token);
        }

    }
}
