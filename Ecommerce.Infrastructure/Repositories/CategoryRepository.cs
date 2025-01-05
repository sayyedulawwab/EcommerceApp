using Ecommerce.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class CategoryRepository(ApplicationDbContext dbContext) 
    : Repository<Category, CategoryId>(dbContext), ICategoryRepository
{
}
