using Ecommerce.API.Controllers.Products;

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
}
