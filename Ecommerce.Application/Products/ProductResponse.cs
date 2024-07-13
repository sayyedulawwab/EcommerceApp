using Ecommerce.Application.Reviews;

namespace Ecommerce.Application.Products;
public sealed class ProductResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }
    public Guid ProductCategoryId { get; init; }
    public List<ReviewResponse> Reviews { get; init; } = new List<ReviewResponse>();
    public long TotalRecords { get; init; }
}
