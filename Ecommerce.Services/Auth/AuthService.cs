using Ecommerce.Models.APIModels;
using Ecommerce.Models.EntityModels;
using Ecommerce.Repositories.Abstractions;
using Ecommerce.Services.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;

namespace Ecommerce.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;

        }
        public bool Register(RegisterDTO model)
        {
            var existingUserByUsername = _userRepository.GetByUsernameOrEmail(model.Username);
            var existingUserByEmail = _userRepository.GetByUsernameOrEmail(model.Email);

            if (existingUserByUsername != null || existingUserByEmail != null)
            {
                return false;
            }

            var salt = GenerateSalt();
            var hashedPassword = HashPassword(model.Password, salt);
            var user = new User { Username = model.Username, Email = model.Email, PasswordHash = hashedPassword, PasswordSalt = salt };

            return _userRepository.Add(user);


        }
        public User Login(LoginDTO model)
        {
            User user = null;

            if (!string.IsNullOrEmpty(model.Username))
            {
                user = _userRepository.GetByUsername(model.Username);
            }

            if (user == null)
            {
                return null;
            }

            var hashedPassword = HashPassword(model.Password, user.PasswordSalt);

            if (hashedPassword == user.PasswordHash)
            {
                return user;
            }

            return null;

        }

        private string GenerateSalt()
        {
            byte[] randomBytes = new byte[128 / 8];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using SHA256 sha256Hash = SHA256.Create();
            return Convert.ToBase64String(sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password + salt)));
        }


    }
}
