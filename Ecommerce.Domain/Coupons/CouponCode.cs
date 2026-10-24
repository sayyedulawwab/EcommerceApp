using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Coupons;
public record CouponCode
{
    public const int MinLength = 3;
    public const int MaxLength = 50;

    public static readonly Error Invalid = new("CouponCode.Invalid", "The coupon code must be 3 to 50 letters, digits or hyphens", HttpStatusCode.BadRequest);

    private CouponCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // Codes are stored in upper case, so "save10" and "SAVE10" are the same coupon.
    public static Result<CouponCode> Create(string? value)
    {
        string? code = value?.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(code) || code.Length < MinLength || code.Length > MaxLength || !code.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
        {
            return Result.Failure<CouponCode>(Invalid);
        }

        return new CouponCode(code);
    }
}
