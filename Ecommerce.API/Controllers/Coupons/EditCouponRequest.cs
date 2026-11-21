using System.Text.Json.Serialization;

namespace Ecommerce.API.Controllers.Coupons;

public record EditCouponRequest(
    [property: JsonRequired] int PercentOff,
    [property: JsonRequired] DateTimeOffset StartsOn,
    [property: JsonRequired] DateTimeOffset ExpiresOn,
    [property: JsonRequired] int MaxRedemptions);
