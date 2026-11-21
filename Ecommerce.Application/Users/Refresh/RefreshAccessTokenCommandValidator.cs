using FluentValidation;

namespace Ecommerce.Application.Users.Refresh;
public class RefreshAccessTokenCommandValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    // A real token is 43 characters. The limit only stops oversized input from being hashed.
    public const int MaxLength = 256;

    public RefreshAccessTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty().MaximumLength(MaxLength);
    }
}
