using Ecommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class ProductRepository(ApplicationDbContext dbContext) 
    : Repository<Product, ProductId>(dbContext), IProductRepository
{
}
