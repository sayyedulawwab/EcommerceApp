using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Orders.GetAllOrders;
public record GetAllOrdersQuery() : IQuery<IReadOnlyList<OrderResponse>>;
