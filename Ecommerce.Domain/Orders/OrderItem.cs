using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;

namespace Ecommerce.Domain.Orders;

public sealed class OrderItem : Entity<OrderItemId>
{
    private OrderItem(OrderItemId id, OrderId orderId, ProductId productId, int quantity, DateTime createdOnUtc) : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        CreatedOnUtc = createdOnUtc;
    }

    public OrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public Product Product { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }


    public static OrderItem Create(OrderId orderId, Product product, int quantity, DateTime createdOnUtc)
    {
        var orderItem = new OrderItem(OrderItemId.New(), orderId, product.Id, quantity, createdOnUtc)
        {
            Product = product
        };

        return orderItem;
    }

}
