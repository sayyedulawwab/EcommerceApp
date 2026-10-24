using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Coupons.DeleteCoupon;
internal sealed class DeleteCouponCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteCouponCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DeleteCouponCommand request, CancellationToken cancellationToken)
    {
        Coupon? coupon = await dbContext.Coupons.FirstOrDefaultAsync(coupon => coupon.Id == new CouponId(request.Id), cancellationToken);

        if (coupon is null)
        {
            return Result.Failure<Guid>(CouponErrors.NotFound);
        }

        if (await HasOrdersAsync(coupon.Id, cancellationToken))
        {
            return Result.Failure<Guid>(CouponErrors.HasOrders);
        }

        dbContext.Coupons.Remove(coupon);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // An order placed between the check and the delete hits the foreign key. Report it as the same conflict.
            if (await HasOrdersAsync(coupon.Id, cancellationToken))
            {
                return Result.Failure<Guid>(CouponErrors.HasOrders);
            }

            throw;
        }

        return coupon.Id.Value;
    }

    private Task<bool> HasOrdersAsync(CouponId couponId, CancellationToken cancellationToken)
    {
        return dbContext.Orders.AnyAsync(order => order.CouponId == couponId, cancellationToken);
    }
}
