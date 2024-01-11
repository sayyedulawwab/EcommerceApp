namespace Ecommerce.Domain.Products;
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Product product);
}
