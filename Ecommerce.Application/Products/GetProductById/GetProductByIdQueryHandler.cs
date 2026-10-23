using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Reviews;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Products.GetProductById;
internal sealed class GetProductByIdQueryHandler(
    IApplicationDbContext dbContext)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        Product? product = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(product => product.Id == new ProductId(request.Id), cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductResponse>(ProductErrors.NotFound);
        }

        IReadOnlyList<Review> reviews = await dbContext.Reviews.AsNoTracking().Where(review => review.ProductId == product.Id).ToListAsync(cancellationToken);

        var productResponse = new ProductResponse()
        {
            Id = product.Id.Value,
            Name = product.Name.Value,
            Description = product.Description.Value,
            PriceAmount = product.Price.Amount,
            PriceCurrency = product.Price.Currency.Code,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId.Value,
            CreatedOnUtc = product.CreatedOnUtc,
            UpdatedOnUtc = product.UpdatedOnUtc,
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
