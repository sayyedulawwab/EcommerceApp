using System.Text.Json.Serialization;

namespace Ecommerce.API.Controllers.Reviews.GiveReview;

public record GiveReviewRequest
{
    [JsonRequired] public Guid ProductId { get; init; }
    [JsonRequired] public int Rating { get; init; }
    [JsonRequired] public string Comment { get; init; }
}
