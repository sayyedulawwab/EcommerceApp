using Ecommerce.Application.Services.Auth.Common;
using ErrorOr;

namespace Ecommerce.Application.Services.Auth.Commands
{
    public interface IAuthCommandService
    {
        ErrorOr<AuthResult> Register(string firstName, string lastName, string email, string password);
       
    }
}
