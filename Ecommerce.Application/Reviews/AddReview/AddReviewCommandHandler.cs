using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;
using Ecommerce.Domain.Users;

namespace Ecommerce.Application.Reviews.AddReview;

internal sealed class AddReviewCommandHandler : ICommandHandler<AddReviewCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    public AddReviewCommandHandler(IProductRepository productRepository, IReviewRepository reviewRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productRepository = productRepository;
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }
    public async Task<Result<Guid>> Handle(AddReviewCommand request, CancellationToken cancellationToken)
    {

        Product? product = await _productRepository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken);

        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound);
        }

        Result<Rating> rating = Rating.Create(request.Rating);
        var review = Review.Create(product,
                                   new UserId(request.UserId),
                                   rating.Value,
                                   new Comment(request.Comment),
                                   _dateTimeProvider.UtcNow);

        _reviewRepository.Add(review);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return review.Id.Value;

    }
}
