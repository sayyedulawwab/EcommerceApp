using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class ReviewRepository(ApplicationDbContext dbContext) 
    : Repository<Review, ReviewId>(dbContext), IReviewRepository
{
    public async Task<IReadOnlyList<Review>> GetByProductAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Review>().Where(review => review.ProductId == productId).ToListAsync(cancellationToken);
    }
}
