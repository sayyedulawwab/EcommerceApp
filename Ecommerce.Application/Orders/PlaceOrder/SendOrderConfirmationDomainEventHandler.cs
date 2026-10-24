using System.Globalization;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Orders.Events;
using Ecommerce.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Orders.PlaceOrder;
internal sealed class SendOrderConfirmationDomainEventHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<OrderPlacedDomainEvent>
{
    public async Task Handle(OrderPlacedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // The event carries only the id, so load what the email needs.
        Order? order = await dbContext.Orders.AsNoTracking().FirstOrDefaultAsync(order => order.Id == domainEvent.OrderId, cancellationToken);

        if (order is null)
        {
            return;
        }

        User? user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == order.UserId, cancellationToken);

        if (user is null)
        {
            return;
        }

        string total = order.TotalPrice.Amount.ToString("0.00", CultureInfo.InvariantCulture);

        await emailService.SendAsync(
            user.Email,
            "Your order is placed",
            $"Your order {order.Id.Value} for {total} {order.TotalPrice.Currency.Code} is placed.");
    }
}
