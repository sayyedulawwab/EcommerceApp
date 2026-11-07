using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

        string cacheKey = $"products-{GenerateCacheKey(request)}";

        // On a miss the factory queries the database; concurrent requests for the same key share that one call.
        IReadOnlyList<ProductResponse> products = await cache.GetOrCreateAsync<IReadOnlyList<ProductResponse>>(
            cacheKey,
            async token => await productQueryRepository.SearchAsync(request, page, pageSize, token),
            tags: [ProductCache.Tag],
            cancellationToken: cancellationToken);

        long totalRecords = products.Count > 0 ? products[0].TotalRecords : 0;

        return PagedList<ProductResponse>.Create(products, page, pageSize, totalRecords);
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
