using Ecommerce.Domain.Categories;

namespace Ecommerce.IntegrationTests.Helpers;

internal static class CategoryData
{
    public static Category Create(string name = "Electronics", string code = "ELEC")
    {
        return Category.Create(new CategoryName(name), new CategoryCode(code), DateTime.UtcNow);
    }
}
