namespace Ecommerce.Domain.ProductCategories;
public interface IProductCategoryRepository
{
    Task<ProductCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(ProductCategory productCategory);
}
