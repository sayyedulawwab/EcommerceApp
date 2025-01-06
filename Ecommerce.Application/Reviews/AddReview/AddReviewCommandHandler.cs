using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;
using Ecommerce.Domain.Users;

namespace Ecommerce.Application.Reviews.AddReview;

internal sealed class AddReviewCommandHandler(
    IProductRepository productRepository,
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddReviewCommand request, CancellationToken cancellationToken)
    {

        Product? product = await productRepository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken);

        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound);
        }

        Result<Rating> rating = Rating.Create(request.Rating);
        var review = Review.Create(product,
                                   new UserId(request.UserId),
                                   rating.Value,
                                   new Comment(request.Comment),
                                   dateTimeProvider.UtcNow);

        reviewRepository.Add(review);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return review.Id.Value;
    }
}
