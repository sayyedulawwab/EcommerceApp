using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Products;
public record ProductDescription
{
    public const int MaxLength = 2000;

    public static readonly Error Invalid = new("ProductDescription.Invalid", "The product description is required and can't be longer than 2000 characters", HttpStatusCode.BadRequest);

    private ProductDescription(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<ProductDescription> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return Result.Failure<ProductDescription>(Invalid);
        }

        return new ProductDescription(value);
    }
}
