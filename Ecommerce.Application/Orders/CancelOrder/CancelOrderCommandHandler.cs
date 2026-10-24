using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Orders.CancelOrder;
internal sealed class CancelOrderCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ICacheService cacheService)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        // Another customer's order is reported as not found, so the response doesn't reveal that it exists.
        Order? order = await dbContext.Orders
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(order => order.Id == new OrderId(request.OrderId) && order.UserId == new UserId(request.UserId), cancellationToken);

        if (order is null)
        {
            return Result.Failure(OrderErrors.NotFound);
        }

        var productIds = order.OrderItems.Select(item => item.ProductId).Distinct().ToList();

        Dictionary<ProductId, Product> products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        Coupon? coupon = order.CouponId is null
            ? null
            : await dbContext.Coupons.FirstAsync(coupon => coupon.Id == order.CouponId, cancellationToken);

        Result cancelResult = OrderCancellationService.Cancel(order, products, coupon, dateTimeProvider.UtcNow);

        if (cancelResult.IsFailure)
        {
            return cancelResult;
        }

        // The order, the products and the coupon all have a concurrency token, so a second cancel
        // that read the same order at the same time fails instead of giving the stock back twice.
        await dbContext.SaveChangesAsync(cancellationToken);

        // The product search is cached, and cancelling changed the stock it shows.
        await cacheService.RemoveByPrefixAsync("products", cancellationToken);

        return Result.Success();
    }
}
