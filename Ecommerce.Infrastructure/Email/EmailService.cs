using Ecommerce.Application.Abstractions.Email;

namespace Ecommerce.Infrastructure.Email;
internal sealed class EmailService : IEmailService
{
    public Task SendAsync(Domain.Users.Email email, string subject, string body)
    {
        return Task.CompletedTask;
    }
}
