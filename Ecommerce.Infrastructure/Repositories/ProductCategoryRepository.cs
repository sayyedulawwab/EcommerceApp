using Ecommerce.Domain.ProductCategories;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class ProductCategoryRepository : Repository<ProductCategory, ProductCategoryId>, IProductCategoryRepository
{
    public ProductCategoryRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<List<ProductCategoryId>> GetProductCategoryGuidsAsync()
    {
        // Query the database to retrieve product category GUIDs
        var productCategoryGuids = await DbContext.Set<ProductCategory>().Select(pc => pc.Id).ToListAsync();
        return productCategoryGuids;
    }
}
