using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Users;
public record Email
{
    public const int MaxLength = 200;

    public static readonly Error Invalid = new("Email.Invalid", "The email is required, must be a valid address and can't be longer than 200 characters", HttpStatusCode.BadRequest);

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return Result.Failure<Email>(Invalid);
        }

        // The same check as FluentValidation's EmailAddress(): one '@' with text on both sides.
        int at = value.IndexOf('@', StringComparison.Ordinal);

        if (at <= 0 || at == value.Length - 1 || value.IndexOf('@', at + 1) >= 0)
        {
            return Result.Failure<Email>(Invalid);
        }

        return new Email(value);
    }
}
