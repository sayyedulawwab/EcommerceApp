using Ecommerce.Application.Abstractions.Paging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Application.Products.SearchProduct;
internal sealed class SearchProductQueryHandler(
    IProductQueryRepository productQueryRepository,
    ICacheService cacheService)
    : IQueryHandler<SearchProductQuery, PagedList<ProductResponse>>
{
    public async Task<Result<PagedList<ProductResponse>>> Handle(SearchProductQuery request, CancellationToken cancellationToken)
    {
        int page = request.Page > 0 ? request.Page : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 10;

        string cacheKey = $"products-{GenerateCacheKey(request)}";

        IEnumerable<ProductResponse>? cachedProductResponses = await cacheService.GetAsync<IEnumerable<ProductResponse>?>(cacheKey, cancellationToken);

        if (cachedProductResponses is not null && cachedProductResponses.Any())
        {
            long totalRecordsFromCache = cachedProductResponses.FirstOrDefault()?.TotalRecords ?? 0;
            var pagedProductsFromCache = PagedList<ProductResponse>.Create(cachedProductResponses, page, pageSize, totalRecordsFromCache);
            return pagedProductsFromCache;
        }

        IReadOnlyList<ProductResponse> products = await productQueryRepository.SearchAsync(request, page, pageSize, cancellationToken);

        long totalRecords = products.Count > 0 ? products[0].TotalRecords : 0;

        await cacheService.SetAsync(cacheKey, products, cancellationToken);

        var pagedProducts = PagedList<ProductResponse>.Create(products, page, pageSize, totalRecords);

        return pagedProducts;
    }


    private static string GenerateCacheKey(SearchProductQuery request)
    {
        // Create an anonymous object with all the necessary parameters
        var parameters = new
        {
            request.Page,
            request.PageSize,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            request.Keyword,
            request.SortOrder,
            request.SortColumn
        };

        // Serialize to JSON
        string serializedParams = JsonSerializer.Serialize(parameters);

        // Hash the serialized string (SHA256 example)
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(serializedParams));
        return Convert.ToBase64String(hashBytes);
    }
}
