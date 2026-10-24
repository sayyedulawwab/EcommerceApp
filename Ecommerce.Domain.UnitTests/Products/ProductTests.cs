using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;

namespace Ecommerce.Domain.UnitTests.Products;

public class ProductTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RemoveStock_Should_RemoveQuantity_WhenStockIsEnough()
    {
        Product product = CreateProduct(quantity: 10);

        Result result = product.RemoveStock(4, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, product.Quantity);
        Assert.Equal(UtcNow, product.UpdatedOnUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RemoveStock_Should_ReturnInvalidStockQuantity_WhenQuantityIsNotPositive(int quantity)
    {
        Product product = CreateProduct(quantity: 10);

        Result result = product.RemoveStock(quantity, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InvalidStockQuantity, result.Error);
        Assert.Equal(10, product.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_ReturnInvalidPrice_WhenPriceIsNotPositive(decimal amount)
    {
        Result<Product> result = Product.Create(
            ProductName.Create("Keyboard").Value,
            ProductDescription.Create("Mechanical keyboard").Value,
            new Money(amount, Currency.Usd),
            10,
            CategoryId.New(),
            UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InvalidPrice, result.Error);
    }

    [Fact]
    public void Create_Should_ReturnInvalidQuantity_WhenQuantityIsNegative()
    {
        Result<Product> result = Product.Create(
            ProductName.Create("Keyboard").Value,
            ProductDescription.Create("Mechanical keyboard").Value,
            new Money(100m, Currency.Usd),
            -1,
            CategoryId.New(),
            UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InvalidQuantity, result.Error);
    }

    [Fact]
    public void Update_Should_LeaveProductUnchanged_WhenQuantityIsNegative()
    {
        Product product = CreateProduct(quantity: 10);

        Result result = Product.Update(
            product,
            ProductName.Create("Other").Value,
            product.Description,
            product.Price,
            -1,
            product.CategoryId,
            UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InvalidQuantity, result.Error);
        Assert.Equal("Keyboard", product.Name.Value);
        Assert.Equal(10, product.Quantity);
    }

    private static Product CreateProduct(int quantity)
    {
        return Product.Create(
            ProductName.Create("Keyboard").Value,
            ProductDescription.Create("Mechanical keyboard").Value,
            new Money(100m, Currency.Usd),
            quantity,
            CategoryId.New(),
            UtcNow).Value;
    }
}
