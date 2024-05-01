using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Reviews.AddReview;

public record AddReviewCommand(Guid productId, Guid userId, int rating, string comment) : ICommand<Guid>;