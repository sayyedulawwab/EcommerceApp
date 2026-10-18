using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.Orders;
public sealed class Order : Entity<OrderId>
{
    private Order(OrderId id, UserId userId, Money totalPrice, OrderStatus status, DateTime createdOnUtc) : base(id)
    {
        UserId = userId;
        TotalPrice = totalPrice;
        Status = status;
        CreatedOnUtc = createdOnUtc;
    }

    private Order()
    {
    }

    public UserId UserId { get; private set; }
    public Money TotalPrice { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public List<OrderItem> OrderItems { get; private set; } = [];

    public static Order Create(UserId userId, OrderStatus status, DateTime createdOnUtc)
    {
        var order = new Order(OrderId.New(), userId, Money.Zero(), status, createdOnUtc);

        return order;
    }

    public Result AddOrderItem(Product product, int quantity, DateTime createdOnUtc)
    {
        if (quantity <= 0)
        {
            return Result.Failure(OrderErrors.InvalidQuantity);
        }

        var lineTotal = new Money(product.Price.Amount * quantity, product.Price.Currency);

        if (OrderItems.Count > 0 && TotalPrice.Currency != lineTotal.Currency)
        {
            return Result.Failure(OrderErrors.CurrencyMismatch);
        }

        TotalPrice = OrderItems.Count == 0 ? lineTotal : TotalPrice + lineTotal;

        OrderItems.Add(OrderItem.Create(Id, product, quantity, createdOnUtc));

        return Result.Success();
    }

    public static Order Update(Order order, UserId userId, Money totalPrice, OrderStatus status, DateTime updatedOnUtc)
    {
        order.UserId = userId;
        order.TotalPrice = totalPrice;
        order.Status = status;
        order.UpdatedOnUtc = updatedOnUtc;

        return order;
    }
}
