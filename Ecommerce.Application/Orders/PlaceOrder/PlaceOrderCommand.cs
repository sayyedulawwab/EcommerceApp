using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Orders.PlaceOrder;
public record PlaceOrderProductCommand(Guid productId, int quantity);
public record PlaceOrderCommand(Guid userId, List<PlaceOrderProductCommand> orderItems) : ICommand<Guid>;
