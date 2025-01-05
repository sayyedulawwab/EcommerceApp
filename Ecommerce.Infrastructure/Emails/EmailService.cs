using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Domain.Users;

namespace Ecommerce.Infrastructure.Emails;
internal sealed class EmailService : IEmailService
{
    public Task SendAsync(Email email, string subject, string body)
    {
        return Task.CompletedTask;
    }
}
