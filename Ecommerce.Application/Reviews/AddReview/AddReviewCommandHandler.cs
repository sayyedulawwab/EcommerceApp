using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Reviews.AddReview;

internal sealed class AddReviewCommandHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : ICommandHandler<AddReviewCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AddReviewCommand request, CancellationToken cancellationToken)
    {

        Product? product = await dbContext.Products.FirstOrDefaultAsync(product => product.Id == new ProductId(request.ProductId), cancellationToken);

        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound);
        }

        Result<Rating> rating = Rating.Create(request.Rating);
        Result<Comment> comment = Comment.Create(request.Comment);

        if (rating.IsFailure || comment.IsFailure)
        {
            return Result.Failure<Guid>(ValidationError.Collect(rating, comment));
        }

        var review = Review.Write(
            product.Id,
            new UserId(request.UserId),
            rating.Value,
            comment.Value,
            timeProvider.GetUtcNow().UtcDateTime);

        dbContext.Reviews.Add(review);

        await dbContext.SaveChangesAsync(cancellationToken);

        return review.Id.Value;
    }
}
