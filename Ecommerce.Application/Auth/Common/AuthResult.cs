using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Auth.Common
{
    public record AuthResult(User User, string Token);
}
