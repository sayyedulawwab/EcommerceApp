using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Shared;
public record Money(decimal Amount, Currency Currency)
{
    public static readonly Error CurrencyMismatch = new("Money.CurrencyMismatch", "Amounts in different currencies can't be added", HttpStatusCode.BadRequest);

    public Result<Money> Add(Money other)
    {
        if (Currency != other.Currency)
        {
            return Result.Failure<Money>(CurrencyMismatch);
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public Result<Money> Subtract(Money other)
    {
        if (Currency != other.Currency)
        {
            return Result.Failure<Money>(CurrencyMismatch);
        }

        return new Money(Amount - other.Amount, Currency);
    }

    public static Money Zero() => new(0, Currency.None);

    public static Money Zero(Currency currency) => new(0, currency);

    public bool IsZero() => this == Zero(Currency);
}
