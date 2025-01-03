using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Reviews.AddReview;

public record AddReviewCommand(Guid ProductId, Guid UserId, int Rating, string Comment) : ICommand<Guid>;
