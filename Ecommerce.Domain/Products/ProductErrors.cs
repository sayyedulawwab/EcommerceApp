using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Products;
public static class ProductErrors
{
    public static readonly Error NotFound = new(
       "Product.NotFound",
       "The product with the specified identifier was not found",
       HttpStatusCode.NotFound);

    public static readonly Error InvalidStockQuantity = new(
       "Product.InvalidStockQuantity",
       "The quantity of stock to remove must be greater than zero",
       HttpStatusCode.BadRequest);

    public static readonly Error InsufficientStock = new(
       "Product.InsufficientStock",
       "The product doesn't have enough stock for the requested quantity",
       HttpStatusCode.BadRequest);
}
