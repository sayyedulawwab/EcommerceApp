using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Users;
public record FirstName
{
    public const int MaxLength = 200;

    public static readonly Error Invalid = new("FirstName.Invalid", "The first name is required and can't be longer than 200 characters", HttpStatusCode.BadRequest);

    private FirstName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<FirstName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return Result.Failure<FirstName>(Invalid);
        }

        return new FirstName(value);
    }
}
