using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.ProductCategories;
public static class ProductCategoryErrors
{
    public static Error NotFound = new(
       "Property.Found",
       "The property with the specified identifier was not found");
}
