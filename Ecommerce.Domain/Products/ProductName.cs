using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Products;
public record ProductName
{
    public const int MaxLength = 200;

    public static readonly Error Invalid = new("ProductName.Invalid", "The product name is required and can't be longer than 200 characters", HttpStatusCode.BadRequest);

    private ProductName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<ProductName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return Result.Failure<ProductName>(Invalid);
        }

        return new ProductName(value);
    }
}
