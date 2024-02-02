using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.Orders;
public sealed class Order : Entity<OrderId>
{
    public Order(OrderId id, UserId userID, Money totalPrice, OrderStatus status, DateTime createdOn) : base(id)
    {
        UserId = userID;
        TotalPrice = totalPrice;
        Status = status;
        CreatedOnUtc = createdOn;
     
    }

    private Order()
    {
    }

    public UserId UserId { get; private set; }
    public Money TotalPrice { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ShippedOnUtc { get; private set; }
    public DateTime? DeliveredOnUtc { get; private set; }
    public DateTime? CancelledOnUtc { get; private set; }
    public List<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();


    public static Order PlaceOrder(UserId userId, List<(Product product, int quantity)> orderItems, OrderStatus status, DateTime createdOnUtc)
    {
        var order = new Order(OrderId.New(), userId, Money.Zero(), status, createdOnUtc);

        foreach (var (product, quantity) in orderItems)
        {
            Money productPrice = new Money(product.Price.Amount, product.Price.Currency);

            var orderItem = OrderItem.Create(order.Id, product, quantity, createdOnUtc);

            order.OrderItems.Add(orderItem);

            if (order.TotalPrice.Currency != productPrice.Currency)
            {
                throw new InvalidOperationException("Currencies have to be equal");
            }

            order.TotalPrice += new Money(productPrice.Amount * quantity, productPrice.Currency);
        }


        return order;
    }


}
