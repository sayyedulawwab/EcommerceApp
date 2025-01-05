using System.Text.Json.Serialization;

namespace Ecommerce.API.Controllers.Products.SearchProduct;

public record SearchProductRequest
{
    public Guid? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Keyword { get; init; }
    [JsonRequired] public int Page { get; init; }
    [JsonRequired] public int PageSize { get; init; }
    public string? SortColumn { get; init; }
    public string? SortOrder { get; init; }
}
