using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Orders.Events;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.Orders;
public sealed class Order : Entity<OrderId>
{
    private readonly List<OrderItem> _orderItems = [];

    private Order(OrderId id, UserId userId, Money totalPrice, OrderStatus status, DateTime createdOnUtc) : base(id)
    {
        UserId = userId;
        TotalPrice = totalPrice;
        Discount = totalPrice;
        Status = status;
        CreatedOnUtc = createdOnUtc;
    }

    #pragma warning disable CS8618 // Used by EF Core to materialize the entity
    private Order()
    {
    }
    #pragma warning restore CS8618

    public UserId UserId { get; private set; }
    // What the customer pays: the sum of the lines minus the discount.
    public Money TotalPrice { get; private set; }
    public Money Discount { get; private set; }
    public CouponId? CouponId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public DateTime? CancelledOnUtc { get; private set; }
    // Read-only from outside, so lines can only be added through AddOrderItem. EF Core maps the _orderItems field.
    public IReadOnlyList<OrderItem> OrderItems => _orderItems;

    // A new order is always a placed order, so the event below always matches its status.
    public static Order Create(UserId userId, DateTime createdOnUtc)
    {
        var order = new Order(OrderId.New(), userId, Money.Zero(), OrderStatus.Placed, createdOnUtc);

        order.RaiseDomainEvent(new OrderPlacedDomainEvent(order.Id));

        return order;
    }

    public Result AddOrderItem(Product product, int quantity, DateTime createdOnUtc)
    {
        if (quantity <= 0)
        {
            return Result.Failure(OrderErrors.InvalidQuantity);
        }

        var lineTotal = new Money(product.Price.Amount * quantity, product.Price.Currency);

        if (OrderItems.Count == 0)
        {
            // The first line sets the order's currency.
            TotalPrice = lineTotal;
            Discount = Money.Zero(lineTotal.Currency);
        }
        else
        {
            Result<Money> total = TotalPrice.Add(lineTotal);

            if (total.IsFailure)
            {
                return Result.Failure(OrderErrors.CurrencyMismatch);
            }

            TotalPrice = total.Value;
        }

        _orderItems.Add(OrderItem.Create(Id, product, quantity, createdOnUtc));

        return Result.Success();
    }

    // Internal so only a domain service applies a discount: the coupon's redemption must change in the same step.
    internal void ApplyDiscount(CouponId couponId, Money discount)
    {
        if (CouponId is not null)
        {
            throw new InvalidOperationException("The order already has a coupon.");
        }

        CouponId = couponId;
        Discount = discount;
        // Same currency, because the discount is calculated from this total.
        TotalPrice = TotalPrice.Subtract(discount).Value;
    }

    // Internal so an order is only cancelled through OrderCancellationService, which also gives back the stock and the coupon.
    internal Result Cancel(DateTime utcNow)
    {
        if (Status != OrderStatus.Placed)
        {
            return Result.Failure(OrderErrors.CannotCancel);
        }

        Status = OrderStatus.Cancelled;
        CancelledOnUtc = utcNow;
        UpdatedOnUtc = utcNow;

        return Result.Success();
    }
}
