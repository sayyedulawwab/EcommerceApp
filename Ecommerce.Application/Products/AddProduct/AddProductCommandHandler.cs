using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Products.AddProduct;
internal sealed class AddProductCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    HybridCache cache,
    ILogger<AddProductCommandHandler> logger)
    : ICommandHandler<AddProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        Result<ProductName> name = ProductName.Create(request.Name);
        Result<ProductDescription> description = ProductDescription.Create(request.Description);
        Result<Currency> currency = Currency.FromCode(request.PriceCurrency);

        if (name.IsFailure || description.IsFailure || currency.IsFailure)
        {
            return Result.Failure<Guid>(ValidationError.FromResults([name, description, currency]));
        }

        Result<Product> productResult = Product.Create(
            name.Value,
            description.Value,
            new Money(request.PriceAmount, currency.Value),
            request.Quantity,
            new CategoryId(request.CategoryId),
            dateTimeProvider.UtcNow);

        if (productResult.IsFailure)
        {
            return Result.Failure<Guid>(productResult.Error);
        }

        Product product = productResult.Value;

        dbContext.Products.Add(product);

        await dbContext.SaveChangesAsync(cancellationToken);

        await ProductCache.ClearAsync(cache, logger, cancellationToken);

        return product.Id.Value;

    }
}
