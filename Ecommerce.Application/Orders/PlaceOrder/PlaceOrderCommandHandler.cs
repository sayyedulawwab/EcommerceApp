using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Products;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Orders.PlaceOrder;
internal sealed class PlaceOrderCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    HybridCache cache,
    ILogger<PlaceOrderCommandHandler> logger)
    : ICommandHandler<PlaceOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        // Load every product in the order with one query, instead of one query per item.
        var productIds = request.OrderStockItems.Select(item => new ProductId(item.ProductId)).ToList();

        Dictionary<ProductId, Product> products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        List<(Product Product, int Quantity)> lines = [];

        foreach (OrderStockItem item in request.OrderStockItems)
        {
            if (!products.TryGetValue(new ProductId(item.ProductId), out Product? product))
            {
                return Result.Failure<Guid>(ProductErrors.NotFound);
            }

            lines.Add((product, item.Quantity));
        }

        Coupon? coupon = null;

        if (request.CouponCode is not null)
        {
            Result<CouponCode> couponCode = CouponCode.Create(request.CouponCode);

            if (couponCode.IsFailure)
            {
                return Result.Failure<Guid>(couponCode.Error);
            }

            coupon = await dbContext.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode.Value, cancellationToken);

            if (coupon is null)
            {
                return Result.Failure<Guid>(CouponErrors.NotFound);
            }
        }

        Result<Order> placeResult = OrderPlacementService.Place(new UserId(request.UserId), lines, coupon, dateTimeProvider.UtcNow);

        if (placeResult.IsFailure)
        {
            return Result.Failure<Guid>(placeResult.Error);
        }

        Order order = placeResult.Value;

        dbContext.Orders.Add(order);

        await dbContext.SaveChangesAsync(cancellationToken);

        // The product search is cached, and placing an order changed the stock it shows.
        await ProductCache.ClearAsync(cache, logger, cancellationToken);

        return order.Id.Value;
    }
}
