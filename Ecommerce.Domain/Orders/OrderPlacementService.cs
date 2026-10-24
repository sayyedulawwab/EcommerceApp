using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.Orders;

// Placing an order changes up to three aggregates: the order gets its lines, each product loses stock,
// and the coupon counts a redemption. The discount depends on the coupon's pricing rule, which is stored data,
// not part of the order. None of the aggregates owns this rule, so it lives here.
public static class OrderPlacementService
{
    public static Result<Order> Place(UserId userId, IReadOnlyList<(Product Product, int Quantity)> lines, Coupon? coupon, DateTime utcNow)
    {
        if (lines.Count == 0)
        {
            return Result.Failure<Order>(OrderErrors.Empty);
        }

        var order = Order.Create(userId, utcNow);

        foreach ((Product product, int quantity) in lines)
        {
            // AddOrderItem checks the quantity and the currency first, so stock is only removed for a valid line.
            Result addItemResult = order.AddOrderItem(product, quantity, utcNow);

            if (addItemResult.IsFailure)
            {
                return Result.Failure<Order>(addItemResult.Error);
            }

            Result removeStockResult = product.RemoveStock(quantity, utcNow);

            if (removeStockResult.IsFailure)
            {
                return Result.Failure<Order>(removeStockResult.Error);
            }
        }

        if (coupon is not null)
        {
            Result redeemResult = coupon.Redeem(utcNow);

            if (redeemResult.IsFailure)
            {
                return Result.Failure<Order>(redeemResult.Error);
            }

            order.ApplyDiscount(coupon.Id, coupon.CalculateDiscount(order.TotalPrice));
        }

        // On a failure above, earlier products have already lost stock in memory.
        // That's safe only because the caller doesn't save on a failure.
        return order;
    }
}
