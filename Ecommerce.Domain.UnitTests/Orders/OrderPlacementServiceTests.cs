using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.UnitTests.Orders;

public class OrderPlacementServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Place_Should_AddLinesAndRemoveStock_WhenEveryProductHasEnoughStock()
    {
        Product keyboard = CreateProduct(100m, Currency.Usd, quantity: 10);
        Product mouse = CreateProduct(25m, Currency.Usd, quantity: 3);

        Result<Order> result = OrderPlacementService.Place(UserId.New(), [(keyboard, 2), (mouse, 3)], coupon: null, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.OrderItems.Count);
        Assert.Equal(275m, result.Value.TotalPrice.Amount);
        Assert.Equal(8, keyboard.Quantity);
        Assert.Equal(0, mouse.Quantity);
    }

    [Fact]
    public void Place_Should_ReturnInsufficientStock_WhenAProductHasLessStockThanOrdered()
    {
        Product keyboard = CreateProduct(100m, Currency.Usd, quantity: 1);

        Result<Order> result = OrderPlacementService.Place(UserId.New(), [(keyboard, 2)], coupon: null, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InsufficientStock, result.Error);
        Assert.Equal(1, keyboard.Quantity);
    }

    [Fact]
    public void Place_Should_CountEveryLine_WhenTheSameProductIsOrderedTwice()
    {
        Product keyboard = CreateProduct(100m, Currency.Usd, quantity: 3);

        Result<Order> result = OrderPlacementService.Place(UserId.New(), [(keyboard, 2), (keyboard, 2)], coupon: null, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InsufficientStock, result.Error);
    }

    [Fact]
    public void Place_Should_NotRemoveStock_WhenTheLineIsInvalid()
    {
        Product usdKeyboard = CreateProduct(100m, Currency.Usd, quantity: 10);
        Product bdtCable = CreateProduct(500m, Currency.Bdt, quantity: 10);

        Result<Order> result = OrderPlacementService.Place(UserId.New(), [(usdKeyboard, 1), (bdtCable, 1)], coupon: null, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CurrencyMismatch, result.Error);
        Assert.Equal(10, bdtCable.Quantity);
    }

    [Fact]
    public void Place_Should_ReturnEmpty_WhenThereAreNoLines()
    {
        Result<Order> result = OrderPlacementService.Place(UserId.New(), [], coupon: null, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.Empty, result.Error);
    }

    [Fact]
    public void Place_Should_StoreThePriceAtPlacement_WhenTheProductPriceChangesLater()
    {
        Product keyboard = CreateProduct(100m, Currency.Usd, quantity: 10);

        Order order = OrderPlacementService.Place(UserId.New(), [(keyboard, 1)], coupon: null, UtcNow).Value;
        Product.Update(keyboard, keyboard.Name, keyboard.Description, new Money(150m, Currency.Usd), keyboard.Quantity, keyboard.CategoryId, UtcNow);

        Assert.Equal(100m, order.OrderItems[0].UnitPrice.Amount);
    }

    [Fact]
    public void Place_Should_ApplyTheDiscountAndRedeemTheCoupon_WhenTheCouponIsValid()
    {
        Product keyboard = CreateProduct(99.95m, Currency.Usd, quantity: 10);
        Coupon coupon = CreateCoupon(percentOff: 10, maxRedemptions: 5);

        Order order = OrderPlacementService.Place(UserId.New(), [(keyboard, 1)], coupon, UtcNow).Value;

        // 10% of 99.95 is 9.995, rounded half away from zero to 10.00.
        Assert.Equal(10.00m, order.Discount.Amount);
        Assert.Equal(89.95m, order.TotalPrice.Amount);
        Assert.Equal(coupon.Id, order.CouponId);
        Assert.Equal(1, coupon.RedemptionCount);
    }

    [Fact]
    public void Place_Should_HaveNoDiscount_WhenThereIsNoCoupon()
    {
        Product keyboard = CreateProduct(100m, Currency.Usd, quantity: 10);

        Order order = OrderPlacementService.Place(UserId.New(), [(keyboard, 1)], coupon: null, UtcNow).Value;

        Assert.Equal(Money.Zero(Currency.Usd), order.Discount);
        Assert.Null(order.CouponId);
    }

    [Fact]
    public void Place_Should_ReturnUsageLimitReached_WhenTheCouponIsUsedUp()
    {
        Product keyboard = CreateProduct(100m, Currency.Usd, quantity: 10);
        Coupon coupon = CreateCoupon(percentOff: 10, maxRedemptions: 1);
        coupon.Redeem(UtcNow);

        Result<Order> result = OrderPlacementService.Place(UserId.New(), [(keyboard, 1)], coupon, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CouponErrors.UsageLimitReached, result.Error);
    }

    [Fact]
    public void Place_Should_NotRedeemTheCoupon_WhenStockIsInsufficient()
    {
        Product keyboard = CreateProduct(100m, Currency.Usd, quantity: 1);
        Coupon coupon = CreateCoupon(percentOff: 10, maxRedemptions: 5);

        Result<Order> result = OrderPlacementService.Place(UserId.New(), [(keyboard, 2)], coupon, UtcNow);

        Assert.Equal(ProductErrors.InsufficientStock, result.Error);
        Assert.Equal(0, coupon.RedemptionCount);
    }

    internal static Coupon CreateCoupon(int percentOff, int maxRedemptions)
    {
        return Coupon.Create(CouponCode.Create("SAVE10").Value, percentOff, UtcNow.AddDays(-1), UtcNow.AddDays(30), maxRedemptions, UtcNow).Value;
    }

    internal static Product CreateProduct(decimal priceAmount, Currency currency, int quantity)
    {
        return Product.Create(
            ProductName.Create("Keyboard").Value,
            ProductDescription.Create("Mechanical keyboard").Value,
            new Money(priceAmount, currency),
            quantity,
            CategoryId.New(),
            UtcNow).Value;
    }
}
