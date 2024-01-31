using Ecommerce.Domain.Products;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class ProductRepository : Repository<Product, ProductId>, IProductRepository
{
    public ProductRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}
