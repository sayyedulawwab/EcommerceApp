using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Application.Abstractions.Auth;
public interface IJwtService
{
    Result<string> GetAccessToken(string email, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
}
