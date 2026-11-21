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
        string? trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength)
        {
            return Result.Failure<Email>(Invalid);
        }

        // The same check as FluentValidation's EmailAddress(): one '@' with text on both sides.
        int at = trimmed.IndexOf('@', StringComparison.Ordinal);

        if (at <= 0 || at == trimmed.Length - 1 || trimmed.IndexOf('@', at + 1) >= 0)
        {
            return Result.Failure<Email>(Invalid);
        }

        // Users expect an email to be case-insensitive. Storing one lowercase form lets the unique index
        // on users.email also catch addresses that differ only in case.
#pragma warning disable CA1308 // Lowercase is the stored form on purpose; the rule prefers uppercase for comparisons.
        return new Email(trimmed.ToLowerInvariant());
#pragma warning restore CA1308
    }
}
