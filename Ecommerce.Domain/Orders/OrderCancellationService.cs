using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Products;

namespace Ecommerce.Domain.Orders;

// Cancelling undoes what placing did, across the same aggregates: the order is cancelled,
// each product gets its stock back, and the coupon gets its redemption back.
public static class OrderCancellationService
{
    public static Result Cancel(Order order, IReadOnlyDictionary<ProductId, Product> products, Coupon? coupon, DateTime utcNow)
    {
        if (order.CouponId != coupon?.Id)
        {
            throw new ArgumentException("The coupon doesn't match the order's coupon.", nameof(coupon));
        }

        // Cancelling twice is a no-op, so a retried request is safe and nothing is given back twice.
        if (order.Status == OrderStatus.Cancelled)
        {
            return Result.Success();
        }

        Result cancelResult = order.Cancel(utcNow);

        if (cancelResult.IsFailure)
        {
            return cancelResult;
        }

        foreach (OrderItem item in order.OrderItems)
        {
            // Order lines reference their product, so a missing one means the caller didn't load it: a bug, not a business outcome.
            if (!products.TryGetValue(item.ProductId, out Product? product))
            {
                throw new ArgumentException($"Product {item.ProductId.Value} of the order wasn't loaded.", nameof(products));
            }

            // Every line was saved with a positive quantity, so this can't fail.
            product.AddStock(item.Quantity, utcNow);
        }

        coupon?.ReleaseRedemption(utcNow);

        return Result.Success();
    }
}
