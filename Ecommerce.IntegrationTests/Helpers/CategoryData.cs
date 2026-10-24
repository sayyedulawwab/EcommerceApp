using Ecommerce.Domain.Categories;

namespace Ecommerce.IntegrationTests.Helpers;

internal static class CategoryData
{
    public static Category Create(string name = "Electronics", string code = "ELEC")
    {
        return Category.Create(CategoryName.Create(name).Value, CategoryCode.Create(code).Value, DateTime.UtcNow);
    }
}
