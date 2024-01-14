using Ecommerce.Domain.ProductCategories;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class ProductCategoryRepository : Repository<ProductCategory>, IProductCategoryRepository
{
    public ProductCategoryRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}
