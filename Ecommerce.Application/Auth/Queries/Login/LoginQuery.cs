using Ecommerce.Application.Auth.Common;
using ErrorOr;
using MediatR;

namespace Ecommerce.Application.Auth.Queries.Login
{
    public record LoginQuery(
        string Email,
        string Password) : IRequest<ErrorOr<AuthResult>>;

}
