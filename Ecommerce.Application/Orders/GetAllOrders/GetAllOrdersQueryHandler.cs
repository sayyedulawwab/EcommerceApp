using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Orders.GetAllOrders;
internal sealed class GetAllOrdersQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAllOrdersQuery, IReadOnlyList<OrderResponse>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> orders = await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == new UserId(request.UserId))
            .Include(order => order.OrderItems)
            .ThenInclude(orderItem => orderItem.Product)
            .ToListAsync(cancellationToken);

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
                PriceAmount = oi.UnitPrice.Amount,
                PriceCurrency = oi.UnitPrice.Currency.Code,
                CategoryId = oi.Product.CategoryId.Value,
                Quantity = oi.Quantity,
                CreatedOnUtc = oi.CreatedOnUtc
            }).ToList(),
            TotalPriceAmount = order.TotalPrice.Amount,
            TotalPriceCurrency = order.TotalPrice.Currency.Code,
            DiscountAmount = order.Discount.Amount,
            Status = order.Status.ToString(),
            CreatedOnUtc = order.CreatedOnUtc,
            CancelledOnUtc = order.CancelledOnUtc
        });

        return ordersResponse.ToList();
    }
}
