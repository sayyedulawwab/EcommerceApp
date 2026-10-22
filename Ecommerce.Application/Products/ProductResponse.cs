using Ecommerce.Application.Reviews;

namespace Ecommerce.Application.Products;
public sealed class ProductResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal PriceAmount { get; init; }
    public required string PriceCurrency { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public Guid CategoryId { get; init; }
    public List<ReviewResponse> Reviews { get; init; } = [];
    public long TotalRecords { get; init; }
}
