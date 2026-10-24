using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Coupons.EditCoupon;
internal sealed class EditCouponCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditCouponCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EditCouponCommand request, CancellationToken cancellationToken)
    {
        Coupon? coupon = await dbContext.Coupons.FirstOrDefaultAsync(coupon => coupon.Id == new CouponId(request.Id), cancellationToken);

        if (coupon is null)
        {
            return Result.Failure<Guid>(CouponErrors.NotFound);
        }

        Result updateResult = coupon.Update(request.PercentOff, request.StartsOnUtc, request.ExpiresOnUtc, request.MaxRedemptions, dateTimeProvider.UtcNow);

        if (updateResult.IsFailure)
        {
            return Result.Failure<Guid>(updateResult.Error);
        }

        // The coupon's concurrency token fails this save if an order redeemed it after it was loaded.
        await dbContext.SaveChangesAsync(cancellationToken);

        return coupon.Id.Value;
    }
}
