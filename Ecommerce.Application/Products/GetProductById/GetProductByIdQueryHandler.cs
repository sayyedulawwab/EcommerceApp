using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Orders.GetAllOrders;
using Ecommerce.Application.Reviews;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;

namespace Ecommerce.Application.Products.GetProductById;
internal sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    private readonly IProductRepository _productRepository;
    private readonly IReviewRepository _reviewRepository;

    public GetProductByIdQueryHandler(IProductRepository productRepository, IReviewRepository reviewRepository)
    {
        _productRepository = productRepository;
        _reviewRepository = reviewRepository;
    }
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(new ProductId(request.id), cancellationToken);


        var reviews = await _reviewRepository.GetByProductAsync(product.Id, cancellationToken);

        var productResponse = new ProductResponse()
        {
            Id = product.Id.Value,
            Name = product.Name.Value,
            Description = product.Description.Value,
            PriceAmount = product.Price.Amount,
            PriceCurrency = product.Price.Currency.Code,
            Quantity = product.Quantity,
            ProductCategoryId = product.ProductCategoryId.Value,
            CreatedOn = product.CreatedOn,
            UpdatedOn = product.UpdatedOn,
            Reviews = reviews.Select(review => new ReviewResponse
            {
                ReviewId = review.Id.Value,
                Rating = review.Rating.Value,
                Comment = review.Comment.Value,
                CreatedOnUtc = review.CreatedOnUtc
            }).ToList(),

        };

        return productResponse;
    }
}
