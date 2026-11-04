using Ecommerce.Application.Categories.EditCategory;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests.Categories;

public class EditCategoryCommandValidatorTests
{
    private static readonly EditCategoryCommandValidator Validator = new();
    private static readonly EditCategoryCommand ValidCommand = new(Guid.CreateVersion7(), "Electronics", "ELEC");

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenCommandIsValid()
    {
        ValidationResult result = Validator.Validate(ValidCommand);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenIdIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Id = Guid.Empty });

        ValidationAssert.HasSingleError(result, nameof(EditCategoryCommand.Id));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenNameIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Name = "" });

        ValidationAssert.HasSingleError(result, nameof(EditCategoryCommand.Name));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenNameIsLongerThan200Characters()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Name = new string('a', 201) });

        ValidationAssert.HasSingleError(result, nameof(EditCategoryCommand.Name));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenCodeIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Code = "" });

        ValidationAssert.HasSingleError(result, nameof(EditCategoryCommand.Code));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenCodeIsLongerThan200Characters()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Code = new string('A', 201) });

        ValidationAssert.HasSingleError(result, nameof(EditCategoryCommand.Code));
    }
}
