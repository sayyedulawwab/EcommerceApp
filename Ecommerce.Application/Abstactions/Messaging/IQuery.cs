using Ecommerce.Domain.Abstractions;
using MediatR;

namespace Ecommerce.Application.Abstactions.Messaging;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
