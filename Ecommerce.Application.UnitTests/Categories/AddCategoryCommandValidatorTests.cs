using Ecommerce.Application.Categories.AddCategory;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests.Categories;

public class AddCategoryCommandValidatorTests
{
    private static readonly AddCategoryCommandValidator Validator = new();
    private static readonly AddCategoryCommand ValidCommand = new("Electronics", "ELEC");

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenCommandIsValid()
    {
        ValidationResult result = Validator.Validate(ValidCommand);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_Should_ReturnError_WhenNameIsEmpty(string name)
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Name = name });

        ValidationAssert.HasSingleError(result, nameof(AddCategoryCommand.Name));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenNameIsLongerThan200Characters()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Name = new string('a', 201) });

        ValidationAssert.HasSingleError(result, nameof(AddCategoryCommand.Name));
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenNameIs200Characters()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Name = new string('a', 200) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenCodeIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Code = "" });

        ValidationAssert.HasSingleError(result, nameof(AddCategoryCommand.Code));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenCodeIsLongerThan200Characters()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Code = new string('A', 201) });

        ValidationAssert.HasSingleError(result, nameof(AddCategoryCommand.Code));
    }
}
