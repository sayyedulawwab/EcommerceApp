using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Orders;
public static class OrderErrors
{
    public static readonly Error InvalidQuantity = new(
        "Order.InvalidQuantity",
        "The order item quantity must be greater than zero",
        HttpStatusCode.BadRequest);

    public static readonly Error CurrencyMismatch = new(
        "Order.CurrencyMismatch",
        "All products in an order must use the same currency",
        HttpStatusCode.BadRequest);
}
