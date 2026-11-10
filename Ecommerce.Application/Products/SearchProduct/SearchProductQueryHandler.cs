using System.Globalization;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Abstractions.Paging;
using Ecommerce.Domain.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace Ecommerce.Application.Products.SearchProduct;
internal sealed class SearchProductQueryHandler(
    IProductQueryRepository productQueryRepository,
    HybridCache cache)
    : IQueryHandler<SearchProductQuery, PagedList<ProductResponse>>
{
    public async Task<Result<PagedList<ProductResponse>>> Handle(SearchProductQuery request, CancellationToken cancellationToken)
    {
        int page = request.Page > 0 ? request.Page : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 10;

        // A keyword is free text, so it would put a new entry in the cache for every distinct word anyone sends.
        // Keyword searches skip the cache; the trigram index keeps them cheap.
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            ProductSearchResult searchResult = await productQueryRepository.SearchAsync(request, page, pageSize, cancellationToken);

            return PagedList<ProductResponse>.Create(searchResult.Items, page, pageSize, searchResult.TotalCount);
        }

        // The key is built from the values the query actually uses (the validator allows only three sort columns),
        // so two requests that return the same page share an entry.
        string sortColumn = request.SortColumn ?? "CreatedOnUtc";
        string sortOrder = string.Equals(request.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        // On a miss the factory queries the database; concurrent requests for the same key share that one call.
        ProductSearchResult result = await cache.GetOrCreateAsync<ProductSearchResult>(
            string.Create(CultureInfo.InvariantCulture, $"products:{page}:{pageSize}:{sortColumn}:{sortOrder}:{request.CategoryId}:{request.MinPrice}:{request.MaxPrice}"),
            async token => await productQueryRepository.SearchAsync(request, page, pageSize, token),
            tags: [ProductCache.Tag],
            cancellationToken: cancellationToken);

        return PagedList<ProductResponse>.Create(result.Items, page, pageSize, result.TotalCount);
    }
}
