using Ecommerce.Application.Reviews.AddReview;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests.Reviews;

public class AddReviewCommandValidatorTests
{
    private static readonly AddReviewCommandValidator Validator = new();
    private static readonly AddReviewCommand ValidCommand = new(Guid.CreateVersion7(), Guid.CreateVersion7(), 4, "Works well.");

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Validate_Should_ReturnNoErrors_WhenRatingIsOneToFive(int rating)
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Rating = rating });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_Should_ReturnError_WhenRatingIsOutsideOneToFive(int rating)
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Rating = rating });

        ValidationAssert.HasSingleError(result, nameof(AddReviewCommand.Rating));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenCommentIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { Comment = "" });

        ValidationAssert.HasSingleError(result, nameof(AddReviewCommand.Comment));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenProductIdIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidCommand with { ProductId = Guid.Empty });

        ValidationAssert.HasSingleError(result, nameof(AddReviewCommand.ProductId));
    }
}
