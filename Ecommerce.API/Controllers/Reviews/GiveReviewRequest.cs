namespace Ecommerce.API.Controllers.Reviews;

public record GiveReviewRequest(Guid productId, int rating, string comment);