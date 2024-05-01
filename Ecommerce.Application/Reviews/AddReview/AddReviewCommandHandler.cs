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

        var product = await _productRepository.GetByIdAsync(new ProductId(request.productId));

        var rating = Rating.Create(request.rating);
        var review = Review.Create(product,
                                   new UserId(request.userId),
                                   rating.Value,
                                   new Comment(request.comment),
                                   _dateTimeProvider.UtcNow);

        _reviewRepository.Add(review);

        await _unitOfWork.SaveChangesAsync();

        return review.Id.Value;

    }
}
