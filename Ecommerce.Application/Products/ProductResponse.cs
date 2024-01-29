using Ecommerce.Domain.Products;

namespace Ecommerce.Application.Products;
public sealed class ProductResponse
{
    public Guid Id { get; init; }
    public ProductName Name { get; init; }
    public ProductDescription Description { get; init; }
    public Money Price { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }
    public Guid ProductCategoryId { get; init; }
}
