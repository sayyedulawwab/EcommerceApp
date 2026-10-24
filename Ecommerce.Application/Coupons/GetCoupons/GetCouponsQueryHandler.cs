using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Coupons.GetCoupons;
internal sealed class GetCouponsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCouponsQuery, IReadOnlyList<CouponResponse>>
{
    public async Task<Result<IReadOnlyList<CouponResponse>>> Handle(GetCouponsQuery request, CancellationToken cancellationToken)
    {
        List<Coupon> coupons = await dbContext.Coupons
            .AsNoTracking()
            .OrderByDescending(coupon => coupon.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        return coupons.Select(coupon => new CouponResponse
        {
            Id = coupon.Id.Value,
            Code = coupon.Code.Value,
            PercentOff = coupon.PercentOff,
            StartsOnUtc = coupon.StartsOnUtc,
            ExpiresOnUtc = coupon.ExpiresOnUtc,
            MaxRedemptions = coupon.MaxRedemptions,
            RedemptionCount = coupon.RedemptionCount
        }).ToList();
    }
}
