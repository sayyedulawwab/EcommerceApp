using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Products;

namespace Ecommerce.Application.Orders.PlaceOrder;
public record PlaceOrderCommand(Guid userId, List<(Product product, int quantity)> orderItems) : ICommand<Guid>;
