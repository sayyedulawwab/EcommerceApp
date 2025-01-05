using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Application.Products.SearchProduct;
internal sealed class SearchProductQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory, 
    ICacheService cacheService) 
    : IQueryHandler<SearchProductQuery, PagedList<ProductResponse>>
{
    public async Task<Result<PagedList<ProductResponse>>> Handle(SearchProductQuery request, CancellationToken cancellationToken)
    {
        //await _cacheService.RemoveByPrefixAsync("products", cancellationToken);

        string cacheKey = $"products-{GenerateCacheKey(request)}";

        IEnumerable<ProductResponse>? cachedProductResponses = await cacheService.GetAsync<IEnumerable<ProductResponse>?>(cacheKey, cancellationToken);

        if (cachedProductResponses is not null && cachedProductResponses.Any())
        {
            long totalRecordsFromCache = cachedProductResponses.FirstOrDefault()?.TotalRecords ?? 0;
            var pagedProductsFromCache = PagedList<ProductResponse>.Create(cachedProductResponses, request.Page, request.PageSize, totalRecordsFromCache);
            return pagedProductsFromCache;
        }

        using IDbConnection connection = sqlConnectionFactory.CreateConnection();

        // Start building the base query
        var sqlBuilder = new StringBuilder(@"
        SELECT
            p.id AS Id,
            p.name AS Name,
            p.description AS Description,
            p.price_amount AS PriceAmount,
            p.price_currency AS PriceCurrency,
            p.quantity AS Quantity,
            p.created_on_utc AS CreatedOnUtc,
            p.updated_on_utc AS UpdatedOnUtc,
            p.category_id AS CategoryId,
            COUNT(*) OVER () AS TotalRecords
        FROM products AS p");

        // List to hold SQL conditions
        var conditions = new List<string>();

        // Dynamic filtering conditions
        if (request.CategoryId.HasValue)
        {
            conditions.Add("p.category_id = @CategoryId");
        }
        if (request.MinPrice.HasValue)
        {
            conditions.Add("p.price_amount >= @MinPrice");
        }
        if (request.MaxPrice.HasValue)
        {
            conditions.Add("p.price_amount <= @MaxPrice");
        }
        if (!string.IsNullOrEmpty(request.Keyword))
        {
            conditions.Add("p.name LIKE '%' || @Keyword || '%'");
        }

        // Add conditions if there are any
        if (conditions.Any())
        {
            sqlBuilder.Append(" WHERE " + string.Join(" AND ", conditions));
        }

        // Handle sorting
        string[] allowedSortColumns = ["Name", "PriceAmount", "CreatedOnUtc"]; // Add all allowed columns here
        string sortColumn = allowedSortColumns.Contains(request.SortColumn) ? request.SortColumn : "CreatedOnUtc"; // Default to CreatedOnUtc if invalid
        string sortOrder = request.SortOrder?.ToLower(CultureInfo.CurrentCulture) == "desc" ? "DESC" : "ASC"; // Default to ASC if invalid

        sqlBuilder.AppendFormat(CultureInfo.InvariantCulture, " ORDER BY {0} {1}", sortColumn, sortOrder);

        // Add pagination
        sqlBuilder.Append(" OFFSET (@PageSize * (@Page - 1)) ROWS FETCH NEXT @PageSize ROWS ONLY;");

        string sql = sqlBuilder.ToString();

        int page = request.Page > 0 ? request.Page : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 10;

        // Execute query with parameters
        IEnumerable<ProductResponse> products = await connection.QueryAsync<ProductResponse>(sql, new
        {
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            request.Keyword,
            Page = page,
            PageSize = pageSize
        });

        long totalRecords = products.FirstOrDefault()?.TotalRecords ?? 0;

        await cacheService.SetAsync(cacheKey, products, cancellationToken);

        var pagedProducts = PagedList<ProductResponse>.Create(products, page, pageSize, totalRecords);

        return pagedProducts;
    }


    private string GenerateCacheKey(SearchProductQuery request)
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
