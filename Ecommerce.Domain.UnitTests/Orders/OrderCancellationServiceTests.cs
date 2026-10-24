using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.UnitTests.Orders;

public class OrderCancellationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Cancel_Should_GiveBackStockAndTheCouponRedemption_WhenTheOrderIsPlaced()
    {
        Product keyboard = OrderPlacementServiceTests.CreateProduct(100m, Currency.Usd, quantity: 10);
        Coupon coupon = OrderPlacementServiceTests.CreateCoupon(percentOff: 10, maxRedemptions: 1);
        Order order = OrderPlacementService.Place(UserId.New(), [(keyboard, 3)], coupon, UtcNow).Value;

        Result result = OrderCancellationService.Cancel(order, ToDictionary(keyboard), coupon, UtcNow.AddHours(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(UtcNow.AddHours(1), order.CancelledOnUtc);
        Assert.Equal(10, keyboard.Quantity);
        Assert.Equal(0, coupon.RedemptionCount);
    }

    [Fact]
    public void Cancel_Should_ChangeNothing_WhenTheOrderIsAlreadyCancelled()
    {
        Product keyboard = OrderPlacementServiceTests.CreateProduct(100m, Currency.Usd, quantity: 10);
        Order order = OrderPlacementService.Place(UserId.New(), [(keyboard, 3)], coupon: null, UtcNow).Value;
        OrderCancellationService.Cancel(order, ToDictionary(keyboard), coupon: null, UtcNow);

        Result result = OrderCancellationService.Cancel(order, ToDictionary(keyboard), coupon: null, UtcNow.AddHours(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(UtcNow, order.CancelledOnUtc);
        Assert.Equal(10, keyboard.Quantity);
    }

    [Fact]
    public void Cancel_Should_Throw_WhenTheCouponIsNotTheOrdersCoupon()
    {
        Product keyboard = OrderPlacementServiceTests.CreateProduct(100m, Currency.Usd, quantity: 10);
        Order order = OrderPlacementService.Place(UserId.New(), [(keyboard, 1)], coupon: null, UtcNow).Value;
        Coupon otherCoupon = OrderPlacementServiceTests.CreateCoupon(percentOff: 10, maxRedemptions: 1);

        Assert.Throws<ArgumentException>(() => OrderCancellationService.Cancel(order, ToDictionary(keyboard), otherCoupon, UtcNow));
    }

    private static Dictionary<ProductId, Product> ToDictionary(params Product[] products)
    {
        return products.ToDictionary(product => product.Id);
    }
}
