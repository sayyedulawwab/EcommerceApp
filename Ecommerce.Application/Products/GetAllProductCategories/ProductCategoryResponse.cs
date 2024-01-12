using Ecommerce.Domain.ProductCategories;

namespace Ecommerce.Application.Products.GetAllProductCategories;
public sealed class ProductCategoryResponse
{
    public Guid Id { get; init; }
    public CategoryName Name { get; init; }
    public CategoryCode Code { get; init; }
}
