using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.GetAllOrders;
internal sealed class GetAllOrdersQueryHandler(IOrderRepository orderRepository) 
    : IQueryHandler<GetAllOrdersQuery, IReadOnlyList<OrderResponse>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> orders = await orderRepository.GetAllAsync(cancellationToken);

        IEnumerable<OrderResponse> ordersResponse = orders.Select(order => new OrderResponse
        {
            Id = order.Id.Value,
            UserId = order.UserId.Value,
            OrderItems = order.OrderItems.Select(oi => new OrderItemResponse
            {
                OrderId = oi.OrderId.Value,
                ProductId = oi.ProductId.Value,
                Name = oi.Product.Name.Value,
                Description = oi.Product.Description.Value,
                PriceAmount = oi.Product.Price.Amount,
                PriceCurrency = oi.Product.Price.Currency.Code,
                CategoryId = oi.Product.CategoryId.Value,
                Quantity = oi.Quantity
            }).ToList(),
            TotalPriceAmount = order.TotalPrice.Amount,
            TotalPriceCurrency = order.TotalPrice.Currency.Code,
            Status = order.Status.ToString(),
            CreatedOnUtc = order.CreatedOnUtc
        });

        return ordersResponse.ToList();
    }
}
