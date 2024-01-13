using Ecommerce.Domain.Abstractions;
using MediatR;

namespace Ecommerce.Application.Abstractions.Messaging;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
