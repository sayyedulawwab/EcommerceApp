using System.Net;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Coupons;
public static class CouponErrors
{
    public static readonly Error NotFound = new(
        "Coupon.NotFound",
        "The coupon with the specified code was not found",
        HttpStatusCode.NotFound);

    public static readonly Error AlreadyExists = new(
        "Coupon.AlreadyExists",
        "A coupon with this code already exists",
        HttpStatusCode.Conflict);

    public static readonly Error InvalidPercentOff = new(
        "Coupon.InvalidPercentOff",
        "The discount must be from 1 to 100 percent",
        HttpStatusCode.BadRequest);

    public static readonly Error InvalidPeriod = new(
        "Coupon.InvalidPeriod",
        "The coupon must expire after it starts",
        HttpStatusCode.BadRequest);

    public static readonly Error InvalidMaxRedemptions = new(
        "Coupon.InvalidMaxRedemptions",
        "The coupon must allow at least one redemption",
        HttpStatusCode.BadRequest);

    public static readonly Error NotActive = new(
        "Coupon.NotActive",
        "The coupon is not valid yet",
        HttpStatusCode.BadRequest);

    public static readonly Error Expired = new(
        "Coupon.Expired",
        "The coupon has expired",
        HttpStatusCode.BadRequest);

    // A redemption is released when an order is cancelled, so the same request can succeed later.
    public static readonly Error UsageLimitReached = new(
        "Coupon.UsageLimitReached",
        "The coupon has been used the maximum number of times",
        HttpStatusCode.Conflict);
}
