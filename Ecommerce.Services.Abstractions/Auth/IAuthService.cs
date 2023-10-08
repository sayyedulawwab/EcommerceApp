using Ecommerce.Models.APIModels;
using Ecommerce.Models.EntityModels;


namespace Ecommerce.Services.Abstractions.Auth
{
    public interface IAuthService
    {
        int? Register(RegisterDTO model);
        User Login(LoginDTO model);

    }
}
