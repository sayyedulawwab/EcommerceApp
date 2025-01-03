using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Orders.PlaceOrder;
public record PlaceOrderProductCommand(Guid ProductId, int Quantity);
public record PlaceOrderCommand(Guid UserId, List<PlaceOrderProductCommand> OrderItems) : ICommand<Guid>;
