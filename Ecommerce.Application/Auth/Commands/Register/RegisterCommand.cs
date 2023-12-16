using Ecommerce.Application.Auth.Common;
using ErrorOr;
using MediatR;

namespace Ecommerce.Application.Auth.Commands.Register
{
    public record RegisterCommand(
        string Email,
        string Password,
        string FirstName,
        string LastName) : IRequest<ErrorOr<AuthResult>>;

}
