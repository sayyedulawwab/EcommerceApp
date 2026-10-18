using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Categories;
public static class CategoryErrors
{
    public static readonly Error NotFound = new(
       "Category.NotFound",
       "The category with the specified identifier was not found",
       HttpStatusCode.NotFound);
}
