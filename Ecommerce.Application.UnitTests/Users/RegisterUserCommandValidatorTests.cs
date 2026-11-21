using Ecommerce.Application.Users.Register;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests.Users;

public class RegisterUserCommandValidatorTests
{
    private static readonly RegisterUserCommandValidator Validator = new();
    private static readonly RegisterUserCommand ValidCommand = new("Jane", "Doe", "jane@example.com", "password1");

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenCommandIsValid()
    {
        ValidationResult result = Validator.Validate(ValidCommand);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenPasswordIsShorterThan8Characters()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Password = "1234567" });

        ValidationAssert.HasSingleError(result, nameof(RegisterUserCommand.Password));
    }
}
