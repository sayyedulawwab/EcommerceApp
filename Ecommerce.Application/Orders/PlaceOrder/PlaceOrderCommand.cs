using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Orders.PlaceOrder;
public record OrderStockItem(Guid ProductId, int Quantity);
public record PlaceOrderCommand(Guid UserId, List<OrderStockItem> OrderStockItems) : ICommand<Guid>;
