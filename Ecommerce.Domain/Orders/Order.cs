using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.Orders;
public sealed class Order : Entity<OrderId>
{
    public Order(OrderId id, UserId userID, Money totalPrice, OrderStatus status, DateTime createdOnUtc) : base(id)
    {
        UserId = userID;
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
    public DateTime UpdatedOnUtc { get; private set; }
    public List<OrderItem> OrderItems { get; private set; } = [];

    public static Order Create(UserId userId, OrderStatus status, DateTime createdOnUtc)
    {
        var order = new Order(OrderId.New(), userId, Money.Zero(), status, createdOnUtc);

        return order;
    }

    public void AddOrderItem(OrderId orderId, Product product, int quantity, DateTime createdOnUtc)
    {

        var orderItem = OrderItem.Create(orderId, product, quantity, createdOnUtc);

        OrderItems.Add(orderItem);

        if (TotalPrice.IsZero())
        {
            TotalPrice = new Money(orderItem.Product.Price.Amount, orderItem.Product.Price.Currency);
        }
        else
        {
            if (TotalPrice.Currency != orderItem.Product.Price.Currency)
            {
                throw new InvalidOperationException("Currencies have to be equal");
            }

            TotalPrice += new Money(orderItem.Product.Price.Amount * orderItem.Quantity, orderItem.Product.Price.Currency);
        }
    }

    public static Order Update(Order order, UserId userID, Money totalPrice, OrderStatus status, DateTime updatedOnUtc)
    {

        order.UserId = userID;
        order.TotalPrice = totalPrice;
        order.Status = status;
        order.UpdatedOnUtc = updatedOnUtc;

        return order;
    }

}
