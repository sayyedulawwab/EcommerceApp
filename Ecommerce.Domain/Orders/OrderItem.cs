using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;

namespace Ecommerce.Domain.Orders;

public sealed class OrderItem : Entity<OrderItemId>
{
    private OrderItem(OrderItemId id, OrderId orderId, ProductId productId, Money unitPrice, int quantity, DateTime createdOnUtc) : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        UnitPrice = unitPrice;
        Quantity = quantity;
        CreatedOnUtc = createdOnUtc;
    }

    #pragma warning disable CS8618 // Used by EF Core to materialize the entity
    private OrderItem()
    {
    }
    #pragma warning restore CS8618

    public OrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public Product Product { get; private set; } = null!; // Set in Create, or loaded by EF Core
    // The price when the order was placed. The product's price can change later, and the order must not.
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }


    public static OrderItem Create(OrderId orderId, Product product, int quantity, DateTime createdOnUtc)
    {
        var orderItem = new OrderItem(OrderItemId.New(), orderId, product.Id, product.Price, quantity, createdOnUtc)
        {
            Product = product
        };

        return orderItem;
    }

}
