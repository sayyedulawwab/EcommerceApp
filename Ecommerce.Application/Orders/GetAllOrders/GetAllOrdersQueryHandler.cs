using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.ProductCategories;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.ProductCategories;

namespace Ecommerce.Application.Orders.GetAllOrders;
internal sealed class GetAllOrdersQueryHandler : IQueryHandler<GetAllOrdersQuery, IReadOnlyList<OrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    public GetAllOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetAllAsync();

        var ordersResponse = orders.Select(order => new OrderResponse()
        {
            Id = order.Id.Value,
            UserId = order.UserId.Value,
            OrderItems = order.OrderItems,
            TotalPriceAmount = order.TotalPrice.Amount,
            TotalPriceCurrency = order.TotalPrice.Currency.Code,
            Status = order.Status.ToString(),
            CreatedOnUtc = order.CreatedOnUtc,
            ShippedOnUtc = order.ShippedOnUtc,
            DeliveredOnUtc = order.DeliveredOnUtc,
            CancelledOnUtc = order.CancelledOnUtc            
        });

        return ordersResponse.ToList();
    }
}
