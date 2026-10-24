using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Coupons.AddCoupon;
internal sealed class AddCouponCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddCouponCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddCouponCommand request, CancellationToken cancellationToken)
    {
        Result<CouponCode> code = CouponCode.Create(request.Code);

        if (code.IsFailure)
        {
            return Result.Failure<Guid>(code.Error);
        }

        if (await ExistsAsync(code.Value, cancellationToken))
        {
            return Result.Failure<Guid>(CouponErrors.AlreadyExists);
        }

        Result<Coupon> coupon = Coupon.Create(code.Value, request.PercentOff, request.StartsOnUtc, request.ExpiresOnUtc, request.MaxRedemptions, dateTimeProvider.UtcNow);

        if (coupon.IsFailure)
        {
            return Result.Failure<Guid>(coupon.Error);
        }

        dbContext.Coupons.Add(coupon.Value);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two admins adding the same code at once: the unique index rejects the second one.
            if (await ExistsAsync(code.Value, cancellationToken))
            {
                return Result.Failure<Guid>(CouponErrors.AlreadyExists);
            }

            throw;
        }

        return coupon.Value.Id.Value;
    }

    private Task<bool> ExistsAsync(CouponCode code, CancellationToken cancellationToken)
    {
        return dbContext.Coupons.AnyAsync(coupon => coupon.Code == code, cancellationToken);
    }
}
