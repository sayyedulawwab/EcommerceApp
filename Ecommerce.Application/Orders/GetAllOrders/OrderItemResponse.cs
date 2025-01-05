namespace Ecommerce.Application.Orders.GetAllOrders;

public sealed class OrderItemResponse
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; }
    public Guid CategoryId { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}
