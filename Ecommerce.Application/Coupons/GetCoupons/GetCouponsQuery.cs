using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Coupons.GetCoupons;
public record GetCouponsQuery() : IQuery<IReadOnlyList<CouponResponse>>;
