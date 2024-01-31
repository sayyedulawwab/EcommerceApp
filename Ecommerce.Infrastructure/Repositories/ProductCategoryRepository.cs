using Ecommerce.Domain.ProductCategories;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class ProductCategoryRepository : Repository<ProductCategory, ProductCategoryId>, IProductCategoryRepository
{
    public ProductCategoryRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}
