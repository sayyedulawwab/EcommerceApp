using Ecommerce.Application.Users.Refresh;
using FluentValidation;

namespace Ecommerce.Application.Users.Logout;
public class LogoutUserCommandValidator : AbstractValidator<LogoutUserCommand>
{
    public LogoutUserCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty().MaximumLength(RefreshAccessTokenCommandValidator.MaxLength);
    }
}
