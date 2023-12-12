using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Services.Auth.Common
{
    public record AuthResult(User user, string token);
}
