using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Coupons.DeleteCoupon;
public record DeleteCouponCommand(Guid Id) : ICommand<Guid>;
