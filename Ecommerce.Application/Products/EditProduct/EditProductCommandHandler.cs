using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Products.EditProduct;
internal sealed class EditProductCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    HybridCache cache,
    ILogger<EditProductCommandHandler> logger)
    : ICommandHandler<EditProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EditProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await dbContext.Products.FirstOrDefaultAsync(product => product.Id == new ProductId(request.Id), cancellationToken);

        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound);
        }

        Result<ProductName> name = ProductName.Create(request.Name);
        Result<ProductDescription> description = ProductDescription.Create(request.Description);
        Result<Currency> currency = Currency.FromCode(request.PriceCurrency);

        if (name.IsFailure || description.IsFailure || currency.IsFailure)
        {
            return Result.Failure<Guid>(ValidationError.FromResults([name, description, currency]));
        }

        Result updateResult = Product.Update(
            product,
            name.Value,
            description.Value,
            new Money(request.PriceAmount, currency.Value),
            request.Quantity,
            new CategoryId(request.CategoryId),
            dateTimeProvider.UtcNow);

        if (updateResult.IsFailure)
        {
            return Result.Failure<Guid>(updateResult.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await ProductCache.ClearAsync(cache, logger, cancellationToken);

        return product.Id.Value;
    }
}
