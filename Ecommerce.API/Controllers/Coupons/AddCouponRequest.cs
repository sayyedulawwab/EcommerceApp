using System.Text.Json.Serialization;

namespace Ecommerce.API.Controllers.Coupons;

// DateTimeOffset, so a time sent without an offset can't be stored as if it were UTC.
public record AddCouponRequest(
    string Code,
    [property: JsonRequired] int PercentOff,
    [property: JsonRequired] DateTimeOffset StartsOn,
    [property: JsonRequired] DateTimeOffset ExpiresOn,
    [property: JsonRequired] int MaxRedemptions);
