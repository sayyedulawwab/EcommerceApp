using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Auth.Common
{
    public record AuthResult(User user, string token);
}
