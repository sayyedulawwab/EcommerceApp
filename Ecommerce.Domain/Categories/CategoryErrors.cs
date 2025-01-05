using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Categories;
public static class CategoryErrors
{
    public static readonly Error NotFound = new(
       "ProductCategory.NotFound",
       "Product category not found",
       HttpResponseStatusCodes.NotFound);
}
