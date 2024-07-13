using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.ProductCategories;
public static class ProductCategoryErrors
{
    public static Error NotFound = new(
       "ProductCategory.NotFound",
       "Product category not found");
}
