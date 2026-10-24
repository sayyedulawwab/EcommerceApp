using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Orders.Events;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.UnitTests.Orders;

public class OrderTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AddOrderItem_Should_AddLinesAndTotal_WhenProductsHaveSameCurrency()
    {
        Order order = CreateOrder();

        Result first = order.AddOrderItem(CreateProduct(100m, Currency.Usd), 2, UtcNow);
        Result second = order.AddOrderItem(CreateProduct(25m, Currency.Usd), 3, UtcNow);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, order.OrderItems.Count);
        Assert.Equal(275m, order.TotalPrice.Amount);
        Assert.Equal(Currency.Usd, order.TotalPrice.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddOrderItem_Should_ReturnInvalidQuantity_WhenQuantityIsNotPositive(int quantity)
    {
        Order order = CreateOrder();

        Result result = order.AddOrderItem(CreateProduct(100m, Currency.Usd), quantity, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidQuantity, result.Error);
        Assert.Empty(order.OrderItems);
    }

    [Fact]
    public void AddOrderItem_Should_ReturnCurrencyMismatch_WhenProductsHaveDifferentCurrencies()
    {
        Order order = CreateOrder();
        order.AddOrderItem(CreateProduct(100m, Currency.Usd), 1, UtcNow);

        Result result = order.AddOrderItem(CreateProduct(500m, Currency.Bdt), 1, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CurrencyMismatch, result.Error);
        Assert.Single(order.OrderItems);
        Assert.Equal(100m, order.TotalPrice.Amount);
    }

    [Fact]
    public void Create_Should_RaiseOrderPlacedDomainEvent()
    {
        Order order = CreateOrder();

        IDomainEvent domainEvent = Assert.Single(order.GetDomainEvents());
        Assert.Equal(new OrderPlacedDomainEvent(order.Id), domainEvent);
    }

    private static Order CreateOrder()
    {
        return Order.Create(UserId.New(), OrderStatus.Placed, UtcNow);
    }

    private static Product CreateProduct(decimal priceAmount, Currency currency)
    {
        return Product.Create(
            ProductName.Create("Keyboard").Value,
            ProductDescription.Create("Mechanical keyboard").Value,
            new Money(priceAmount, currency),
            10,
            CategoryId.New(),
            UtcNow).Value;
    }
}
