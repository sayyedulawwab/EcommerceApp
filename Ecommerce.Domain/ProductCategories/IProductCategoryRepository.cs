namespace Ecommerce.Domain.ProductCategories;
public interface IProductCategoryRepository
{
    Task<IReadOnlyList<ProductCategory?>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductCategory?> GetByIdAsync(ProductCategoryId id, CancellationToken cancellationToken = default);
    Task<List<ProductCategoryId>> GetProductCategoryGuidsAsync();
    void Add(ProductCategory productCategory);
    void Update(ProductCategory productCategory);
    void Remove(ProductCategory productCategory);
}
