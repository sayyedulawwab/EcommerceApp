using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Categories;
public record CategoryName
{
    public const int MaxLength = 200;

    public static readonly Error Invalid = new("CategoryName.Invalid", "The category name is required and can't be longer than 200 characters", HttpStatusCode.BadRequest);

    private CategoryName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<CategoryName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return Result.Failure<CategoryName>(Invalid);
        }

        return new CategoryName(value);
    }
}
