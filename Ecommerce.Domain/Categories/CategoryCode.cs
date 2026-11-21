using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Categories;
public record CategoryCode
{
    public const int MaxLength = 200;

    public static readonly Error Invalid = new("CategoryCode.Invalid", "The category code is required and can't be longer than 200 characters", HttpStatusCode.BadRequest);

    private CategoryCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<CategoryCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return Result.Failure<CategoryCode>(Invalid);
        }

        return new CategoryCode(value);
    }
}
