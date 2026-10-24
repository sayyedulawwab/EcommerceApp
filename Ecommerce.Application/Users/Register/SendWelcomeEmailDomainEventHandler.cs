using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Users;
using Ecommerce.Domain.Users.Events;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Users.Register;
internal sealed class SendWelcomeEmailDomainEventHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IDomainEventHandler<UserCreatedDomainEvent>
{
    public async Task Handle(UserCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // The event carries only the id, so load what the email needs.
        User? user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == domainEvent.UserId, cancellationToken);

        if (user is null)
        {
            return;
        }

        await emailService.SendAsync(
            user.Email,
            "Welcome to Ecommerce",
            $"Hi {user.FirstName.Value}, your account is ready.");
    }
}
