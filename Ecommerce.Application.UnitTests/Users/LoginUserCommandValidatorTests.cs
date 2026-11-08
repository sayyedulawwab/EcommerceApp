using Ecommerce.Application.Users.Login;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests.Users;

public class LoginUserCommandValidatorTests
{
    private static readonly LoginUserCommandValidator Validator = new();
    private static readonly LoginUserCommand ValidCommand = new("jane@example.com", "password1");

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenCommandIsValid()
    {
        ValidationResult result = Validator.Validate(ValidCommand);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenEmailIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Email = "" });

        ValidationAssert.HasSingleError(result, nameof(LoginUserCommand.Email));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenPasswordIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Password = "" });

        ValidationAssert.HasSingleError(result, nameof(LoginUserCommand.Password));
    }
}
