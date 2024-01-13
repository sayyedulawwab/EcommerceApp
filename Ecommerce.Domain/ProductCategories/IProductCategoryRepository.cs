namespace Ecommerce.Domain.ProductCategories;
public interface IProductCategoryRepository
{
    Task<IReadOnlyList<ProductCategory?>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(ProductCategory productCategory);
    void Update(ProductCategory productCategory);
    void Remove(ProductCategory productCategory);
}
