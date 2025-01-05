using Ecommerce.Domain.Users;

namespace Ecommerce.Application.Abstractions.Emails;
public interface IEmailService
{
    Task SendAsync(Email email, string subject, string body);
}
