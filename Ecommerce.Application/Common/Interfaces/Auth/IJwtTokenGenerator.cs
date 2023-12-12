using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Common.Interfaces.Auth
{
    public interface IJwtTokenGenerator
    {
        string GenerateJwtToken(User user);
    }
}
