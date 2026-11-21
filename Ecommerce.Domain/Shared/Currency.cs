using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Shared;
public record Currency
{
    public static readonly Error Unsupported = new("Currency.Unsupported", "The currency code is not supported", HttpStatusCode.BadRequest);

    public static readonly Currency None = new("");
    public static readonly Currency Usd = new("USD");
    public static readonly Currency Bdt = new("BDT");

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; }

    public static readonly IReadOnlyCollection<Currency> All =
    [
        Usd,
        Bdt
    ];

    public static bool IsSupported(string? code) => All.Any(c => c.Code == code);

    public static Result<Currency> FromCode(string? code)
    {
        Currency? currency = All.FirstOrDefault(c => c.Code == code);

        if (currency is null)
        {
            return Result.Failure<Currency>(Unsupported);
        }

        return currency;
    }
}
