using Ecommerce.Domain.Products;

namespace Ecommerce.Domain.Reviews;
public interface IReviewRepository
{
    Task<IReadOnlyList<Review?>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Review?> GetByIdAsync(ReviewId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Review?>> GetByProductAsync(ProductId productId, CancellationToken cancellationToken = default);

    void Add(Review review);
    void Update(Review review);
    void Remove(Review review);
}
