using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Users;
public record LastName
{
    public const int MaxLength = 200;

    public static readonly Error Invalid = new("LastName.Invalid", "The last name is required and can't be longer than 200 characters", HttpStatusCode.BadRequest);

    private LastName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<LastName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return Result.Failure<LastName>(Invalid);
        }

        return new LastName(value);
    }
}
