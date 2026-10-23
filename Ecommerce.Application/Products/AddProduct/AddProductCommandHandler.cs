using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;

namespace Ecommerce.Application.Products.AddProduct;
internal sealed class AddProductCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ICacheService cacheService)
    : ICommandHandler<AddProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(
            new ProductName(request.Name),
            new ProductDescription(request.Description),
            new Money(request.PriceAmount,
            Currency.FromCode(request.PriceCurrency)),
            request.Quantity,
            new CategoryId(request.CategoryId),
            dateTimeProvider.UtcNow);

        dbContext.Products.Add(product);

        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveByPrefixAsync("products", cancellationToken);

        return product.Id.Value;

    }
}
