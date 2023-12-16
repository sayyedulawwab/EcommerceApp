using Ecommerce.Application.Auth.Commands.Register;
using Ecommerce.Application.Auth.Common;
using Ecommerce.Application.Auth.Queries.Login;
using Ecommerce.Contracts.Auth;
using Mapster;

namespace Ecommerce.API.Mappings
{
    public class AuthMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<RegisterRequest, RegisterCommand>();

            config.NewConfig<LoginRequest, LoginQuery>();

            config.NewConfig<AuthResult, AuthResponse>()
                  .Map(dest => dest, src => src.User);
        }
    }
}
