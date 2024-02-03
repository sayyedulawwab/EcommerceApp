using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Orders.PlaceOrder;
public record PlaceOrderCommand(Guid userId, List<(Guid productId, int quantity)> orderItems) : ICommand<Guid>;
