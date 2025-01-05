using Ecommerce.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class CategoryRepository : Repository<Category, CategoryId>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<List<CategoryId>> GetCategoryGuidsAsync()
    {
        // Query the database to retrieve product category GUIDs
        List<CategoryId> productCategoryGuids = await DbContext.Set<Category>().Select(pc => pc.Id).ToListAsync();
        return productCategoryGuids;
    }
}
