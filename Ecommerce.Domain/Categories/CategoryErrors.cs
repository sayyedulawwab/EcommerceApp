using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Categories;
public static class CategoryErrors
{
    public static readonly Error NotFound = new(
       "Category.NotFound",
       "The category with the specified identifier was not found",
       HttpStatusCode.NotFound);

    // Deleting the category would leave its products without one, so they have to be moved or deleted first.
    public static readonly Error HasProducts = new(
       "Category.HasProducts",
       "The category can't be deleted because it still has products",
       HttpStatusCode.Conflict);
}
