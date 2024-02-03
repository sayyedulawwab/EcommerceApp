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
        return await DbContext.Set<Product>()
            .Where(p => productIds.Contains(p.Id.Value))
            .ToListAsync();
    }
}
