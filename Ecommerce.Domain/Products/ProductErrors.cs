using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Products;
public static class ProductErrors
{
    public static readonly Error NotFound = new(
       "Product.NotFound",
       "The product with the specified identifier was not found",
       HttpStatusCode.NotFound);
}
