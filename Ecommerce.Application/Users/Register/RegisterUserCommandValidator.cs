using Ecommerce.Domain.Users;
using FluentValidation;

namespace Ecommerce.Application.Users.Register;
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(FirstName.MaxLength);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(LastName.MaxLength);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(Email.MaxLength);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
    }
}
