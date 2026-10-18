using Ecommerce.API.Controllers.Products;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;

namespace Ecommerce.IntegrationTests.Helpers;

internal static class ProductData
{
    public static AddProductRequest Create(
        Guid? categoryId = null)
    {
        return new AddProductRequest
        {
            Name = "Mechanical Keyboard",
            Description = "RGB Mechanical Keyboard",
            PriceCurrency = "USD",
            PriceAmount = 99.99m,
            Quantity = 10,
            CategoryId = categoryId ?? Guid.NewGuid()
        };
    }

    public static Product CreateProduct(
        CategoryId categoryId,
        decimal priceAmount = 99.99m,
        string currencyCode = "USD",
        string name = "Mechanical Keyboard")
    {
        return Product.Create(
            new ProductName(name),
            new ProductDescription("RGB Mechanical Keyboard"),
            new Money(priceAmount, Currency.FromCode(currencyCode)),
            10,
            categoryId,
            DateTime.UtcNow);
    }
}
