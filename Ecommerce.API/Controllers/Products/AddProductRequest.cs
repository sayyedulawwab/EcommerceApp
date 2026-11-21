using System.Text.Json.Serialization;

namespace Ecommerce.API.Controllers.Products;

public record AddProductRequest
{
    [JsonRequired] public required string Name { get; init; }
    [JsonRequired] public required string Description { get; init; }
    [JsonRequired] public required string PriceCurrency { get; init; }
    [JsonRequired] public decimal PriceAmount { get; init; }
    [JsonRequired] public int Quantity { get; init; }
    [JsonRequired] public Guid CategoryId { get; init; }
}
