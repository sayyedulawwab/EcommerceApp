using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Orders.PlaceOrder;
internal sealed class PlaceOrderCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<PlaceOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var order = Order.Create(new UserId(request.UserId), OrderStatus.Placed, dateTimeProvider.UtcNow);

        // Load every product in the order with one query, instead of one query per item.
        var productIds = request.OrderStockItems.Select(item => new ProductId(item.ProductId)).ToList();

        Dictionary<ProductId, Product> products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        foreach (OrderStockItem item in request.OrderStockItems)
        {
            if (!products.TryGetValue(new ProductId(item.ProductId), out Product? product))
            {
                return Result.Failure<Guid>(ProductErrors.NotFound);
            }

            Result addItemResult = order.AddOrderItem(product, item.Quantity, dateTimeProvider.UtcNow);

            if (addItemResult.IsFailure)
            {
                return Result.Failure<Guid>(addItemResult.Error);
            }
        }

        dbContext.Orders.Add(order);

        await dbContext.SaveChangesAsync(cancellationToken);

        return order.Id.Value;
    }
}
