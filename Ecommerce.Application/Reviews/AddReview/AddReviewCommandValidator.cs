using FluentValidation;

namespace Ecommerce.Application.Reviews.AddReview;

public class AddReviewCommandValidator : AbstractValidator<AddReviewCommand>
{
    public AddReviewCommandValidator()
    {
        RuleFor(r => r.rating).NotNull().WithMessage("Please provide a rating from 1 to 5");
        RuleFor(r => r.comment).NotEmpty().WithMessage("Please provide a comment.");
    }
}