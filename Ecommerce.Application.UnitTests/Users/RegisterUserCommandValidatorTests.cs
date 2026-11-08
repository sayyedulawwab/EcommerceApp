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
    public void Validate_Should_ReturnError_WhenFirstNameIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { FirstName = "" });

        ValidationAssert.HasSingleError(result, nameof(RegisterUserCommand.FirstName));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenLastNameIsLongerThan200Characters()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { LastName = new string('a', 201) });

        ValidationAssert.HasSingleError(result, nameof(RegisterUserCommand.LastName));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenEmailIsNotAnEmailAddress()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Email = "jane.example.com" });

        ValidationAssert.HasSingleError(result, nameof(RegisterUserCommand.Email));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenPasswordIsShorterThan8Characters()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Password = "1234567" });

        ValidationAssert.HasSingleError(result, nameof(RegisterUserCommand.Password));
    }

    [Fact]
    public void Validate_Should_ReturnErrorsOnlyForPassword_WhenPasswordIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Password = "" });

        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(RegisterUserCommand.Password), error.PropertyName));
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenPasswordIs72Bytes()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Password = new string('a', 72) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenPasswordIsLongerThan72Bytes()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Password = new string('a', 73) });

        ValidationAssert.HasSingleError(result, nameof(RegisterUserCommand.Password));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenPasswordIsShortInCharactersButLongerThan72Bytes()
    {
        // 'é' is 2 bytes in UTF-8, so 37 of them are 74 bytes.
        ValidationResult result = Validator.Validate(ValidCommand with { Password = new string('é', 37) });

        ValidationAssert.HasSingleError(result, nameof(RegisterUserCommand.Password));
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenPasswordIs8Characters()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Password = "12345678" });

        Assert.True(result.IsValid);
    }
}
