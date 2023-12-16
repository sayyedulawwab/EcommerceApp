using Ecommerce.Application.Auth.Common;
using Ecommerce.Application.Common.Interfaces.Auth;
using Ecommerce.Application.Common.Interfaces.Persistence;
using Ecommerce.Domain.Common.Errors;
using Ecommerce.Domain.Entities;
using ErrorOr;
using MediatR;

namespace Ecommerce.Application.Auth.Queries.Login
{
    public class LoginQueryHandler : IRequestHandler<LoginQuery, ErrorOr<AuthResult>>
    {
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IUserRepository _userRepository;


        public LoginQueryHandler(IJwtTokenGenerator jwtTokenGenerator, IUserRepository userRepository)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
            _userRepository = userRepository;
        }
        public async Task<ErrorOr<AuthResult>> Handle(LoginQuery query, CancellationToken cancellationToken)
        {
            // 1. Validate the user exists
            if (_userRepository.GetUserByEmail(query.Email) is not User user)
            {
                return Errors.Auth.InvalidCredentials;
            }

            // 2. password is correct

            if (user.Password != query.Password)
            {
                return Errors.Auth.InvalidCredentials;
            }

            // 3. create JWT token
            var token = _jwtTokenGenerator.GenerateJwtToken(user);

            return new AuthResult(user, token);
        }
    }
}
