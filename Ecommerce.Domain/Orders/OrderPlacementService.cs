using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.Orders;

// Placing an order changes two aggregates: the order gets its lines and each product loses stock.
// Neither aggregate should reach into the other, so the rule lives here.
public static class OrderPlacementService
{
    public static Result<Order> Place(UserId userId, IReadOnlyList<(Product Product, int Quantity)> lines, DateTime utcNow)
    {
        var order = Order.Create(userId, OrderStatus.Placed, utcNow);

        foreach ((Product product, int quantity) in lines)
        {
            // AddOrderItem checks the quantity and the currency first, so stock is only removed for a valid line.
            Result addItemResult = order.AddOrderItem(product, quantity, utcNow);

            if (addItemResult.IsFailure)
            {
                return Result.Failure<Order>(addItemResult.Error);
            }

            Result removeStockResult = product.RemoveStock(quantity, utcNow);

            if (removeStockResult.IsFailure)
            {
                return Result.Failure<Order>(removeStockResult.Error);
            }
        }

        return order;
    }
}
