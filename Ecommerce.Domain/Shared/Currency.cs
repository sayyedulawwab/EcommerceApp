namespace Ecommerce.Domain.Shared;
public record Currency
{
    public static readonly Currency None = new("");
    public static readonly Currency Usd = new("USD");
    public static readonly Currency Bdt = new("BDT");

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; init; }

    public static readonly IReadOnlyCollection<Currency> All =
    [
        Usd,
        Bdt
    ];

    public static bool IsSupported(string? code) => All.Any(c => c.Code == code);

    public static Currency FromCode(string code)
    {
        return All.FirstOrDefault(c => c.Code == code) ?? throw new ArgumentException("The currency code is invalid", nameof(code));
    }
}
