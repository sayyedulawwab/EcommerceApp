using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Orders.CancelOrder;
public record CancelOrderCommand(Guid UserId, Guid OrderId) : ICommand;
