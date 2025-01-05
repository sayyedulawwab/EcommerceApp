using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Products.SearchProduct;
public record SearchProductQuery(
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Keyword,
    int Page,
    int PageSize,
    string? SortColumn,
    string? SortOrder) : IQuery<PagedList<ProductResponse>>;
