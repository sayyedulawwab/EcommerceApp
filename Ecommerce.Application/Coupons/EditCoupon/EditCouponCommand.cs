using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Coupons.EditCoupon;
public record EditCouponCommand(Guid Id, int PercentOff, DateTime StartsOnUtc, DateTime ExpiresOnUtc, int MaxRedemptions) : ICommand<Guid>;
