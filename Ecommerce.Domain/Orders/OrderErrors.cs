using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Orders;
public static class OrderErrors
{
    public static readonly Error NotFound = new(
        "Order.NotFound",
        "The order with the specified identifier was not found",
        HttpStatusCode.NotFound);

    public static readonly Error Empty = new(
        "Order.Empty",
        "An order must have at least one item",
        HttpStatusCode.BadRequest);

    public static readonly Error InvalidQuantity = new(
        "Order.InvalidQuantity",
        "The order item quantity must be greater than zero",
        HttpStatusCode.BadRequest);

    public static readonly Error CurrencyMismatch = new(
        "Order.CurrencyMismatch",
        "All products in an order must use the same currency",
        HttpStatusCode.BadRequest);

    // A paid or delivered order needs a refund, so it can't be cancelled here.
    public static readonly Error CannotCancel = new(
        "Order.CannotCancel",
        "Only an order that is placed and not paid yet can be cancelled",
        HttpStatusCode.Conflict);
}
