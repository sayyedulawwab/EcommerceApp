using Ecommerce.Application.Services.Auth.Common;
using ErrorOr;

namespace Ecommerce.Application.Services.Auth.Queries
{
    public interface IAuthQueryService
    {
        ErrorOr<AuthResult> Login(string email, string password);
    }
}
