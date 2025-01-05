using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Reviews;
public static class ReviewErrors
{
    public static readonly Error NotEligible = new(
        "Review.NotEligible",
        "The review is not eligible because the order is not yet delivered",
        HttpResponseStatusCodes.BadRequest);
}
