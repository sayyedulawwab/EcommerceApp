using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews.Events;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.Reviews;

/// <summary>
/// A customer's rating and comment on a product. It is its own aggregate: it refers to the
/// product and the user by ID, and reviewing never changes the product itself.
/// </summary>
public sealed class Review : AggregateRoot<ReviewId>
{
    private Review(ReviewId id, ProductId productId, UserId userId, Rating rating, Comment comment, DateTime createdOnUtc)
        : base(id)
    {
        ProductId = productId;
        UserId = userId;
        Rating = rating;
        Comment = comment;
        CreatedOnUtc = createdOnUtc;
    }

    public ProductId ProductId { get; private set; }

    public UserId UserId { get; private set; }

    public Rating Rating { get; private set; }

    public Comment Comment { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    // Rating and Comment are already valid value objects, so writing a review can't fail here.
    public static Review Write(ProductId productId, UserId userId, Rating rating, Comment comment, DateTime createdOnUtc)
    {
        var review = new Review(ReviewId.New(), productId, userId, rating, comment, createdOnUtc);

        review.RecordDomainEvent(new ReviewCreatedDomainEvent(review.Id));

        return review;
    }
}
