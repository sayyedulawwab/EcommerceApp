namespace Ecommerce.Domain.Products;
public interface IProductRepository
{
    Task<IReadOnlyList<Product?>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default);
    Task<List<Product>> GetProductsByIdsAsync(List<Guid> productIds);

    void Add(Product product);
    void Update(Product product);
    void Remove(Product product);
}
