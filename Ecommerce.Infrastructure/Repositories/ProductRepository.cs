using Ecommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class ProductRepository : Repository<Product, ProductId>, IProductRepository
{
    public ProductRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }


    public async Task<List<Product>> GetProductsByIdsAsync(List<Guid> productIds)
    {
        // Materialize the initial query by calling ToListAsync()
        var products = await DbContext.Set<Product>()
            .ToListAsync();

        // Filter the products based on productIds in memory
        return products.Where(p => productIds.Contains(p.Id.Value)).ToList();
    }

    public override async Task<Product?> GetByIdAsync(
        ProductId id,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Product>()
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }
}
