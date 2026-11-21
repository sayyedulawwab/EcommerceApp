using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Products.EditProduct;

internal sealed class EditProductCommandHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : ICommandHandler<EditProductCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(EditProductCommand request, CancellationToken cancellationToken)
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
            return Result.Failure<Guid>(ValidationError.Collect(name, description, currency));
        }

        Result updateResult = Product.Update(
            product,
            name.Value,
            description.Value,
            new Money(request.PriceAmount, currency.Value),
            request.Quantity,
            new CategoryId(request.CategoryId),
            timeProvider.GetUtcNow().UtcDateTime);

        if (updateResult.IsFailure)
        {
            return Result.Failure<Guid>(updateResult.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Id.Value;
    }
}
