namespace Ecommerce.API.Controllers.Products;

// Bound from the query string, not JSON, so every value is optional. A missing page or page size gets the handler's default.
public record SearchProductRequest
{
    public Guid? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Keyword { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? SortColumn { get; init; }
    public string? SortOrder { get; init; }
}
