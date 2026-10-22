using System.Text.Json.Serialization;

namespace Ecommerce.API.Controllers.Reviews;

public record GiveReviewRequest
{
    [JsonRequired] public Guid ProductId { get; init; }
    [JsonRequired] public int Rating { get; init; }
    [JsonRequired] public required string Comment { get; init; }
}
