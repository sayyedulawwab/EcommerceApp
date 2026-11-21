using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Coupons.AddCoupon;
public record AddCouponCommand(string Code, int PercentOff, DateTime StartsOnUtc, DateTime ExpiresOnUtc, int MaxRedemptions) : ICommand<Guid>;
