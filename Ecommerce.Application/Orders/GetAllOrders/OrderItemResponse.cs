namespace Ecommerce.Application.Orders.GetAllOrders;

public sealed class OrderItemResponse
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    // The price paid when the order was placed, not the product's current price.
    public decimal PriceAmount { get; init; }
    public required string PriceCurrency { get; init; }
    public Guid CategoryId { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}
