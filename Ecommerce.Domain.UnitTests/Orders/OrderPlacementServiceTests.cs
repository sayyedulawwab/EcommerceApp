using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
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

        Result<Order> result = OrderPlacementService.Place(UserId.New(), [(keyboard, 2), (mouse, 3)], UtcNow);

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

        Result<Order> result = OrderPlacementService.Place(UserId.New(), [(keyboard, 2)], UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InsufficientStock, result.Error);
        Assert.Equal(1, keyboard.Quantity);
    }

    [Fact]
    public void Place_Should_CountEveryLine_WhenTheSameProductIsOrderedTwice()
    {
        Product keyboard = CreateProduct(100m, Currency.Usd, quantity: 3);

        Result<Order> result = OrderPlacementService.Place(UserId.New(), [(keyboard, 2), (keyboard, 2)], UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InsufficientStock, result.Error);
    }

    [Fact]
    public void Place_Should_NotRemoveStock_WhenTheLineIsInvalid()
    {
        Product usdKeyboard = CreateProduct(100m, Currency.Usd, quantity: 10);
        Product bdtCable = CreateProduct(500m, Currency.Bdt, quantity: 10);

        Result<Order> result = OrderPlacementService.Place(UserId.New(), [(usdKeyboard, 1), (bdtCable, 1)], UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CurrencyMismatch, result.Error);
        Assert.Equal(10, bdtCable.Quantity);
    }

    private static Product CreateProduct(decimal priceAmount, Currency currency, int quantity)
    {
        return Product.Create(
            ProductName.Create("Keyboard").Value,
            ProductDescription.Create("Mechanical keyboard").Value,
            new Money(priceAmount, currency),
            quantity,
            CategoryId.New(),
            UtcNow);
    }
}
