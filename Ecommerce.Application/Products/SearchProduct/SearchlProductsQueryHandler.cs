using Dapper;
using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ecommerce.Application.Products.SearchProduct;
internal sealed class SearchlProductsQueryHandler : IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ICacheService _cacheService;
    public SearchlProductsQueryHandler(ISqlConnectionFactory sqlConnectionFactory, ICacheService cacheService)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _cacheService = cacheService;
    }
    public async Task<Result<PagedList<ProductResponse>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        //await _cacheService.RemoveByPrefixAsync("products", cancellationToken);

        var cacheKey = $"products-{GenerateCacheKey(request)}";

        IEnumerable<ProductResponse>? cachedProductResponses = await _cacheService.GetAsync<IEnumerable<ProductResponse>?>(cacheKey, cancellationToken);


        if (cachedProductResponses is not null && cachedProductResponses.Any())
        {
            var totalRecordsFromCache = cachedProductResponses.FirstOrDefault()?.TotalRecords ?? 0;
            var pagedProductsFromCache = await PagedList<ProductResponse>.CreateAsync(cachedProductResponses, request.page, request.pageSize, totalRecordsFromCache);
            return pagedProductsFromCache;
        }



        using var connection = _sqlConnectionFactory.CreateConnection();

        // Start building the base query
        var sqlBuilder = new StringBuilder(@"
        SELECT
            p.id AS Id,
            p.name AS Name,
            p.description AS Description,
            p.price_amount AS PriceAmount,
            p.price_currency AS PriceCurrency,
            p.quantity AS Quantity,
            p.created_on AS CreatedOn,
            p.updated_on AS UpdatedOn,
            p.product_category_id AS ProductCategoryId,
            COUNT(*) OVER () AS TotalRecords
        FROM products AS p");

        // List to hold SQL conditions
        var conditions = new List<string>();

        // Dynamic filtering conditions
        if (request.productCategoryId.HasValue)
        {
            conditions.Add("p.product_category_id = @ProductCategoryId");
        }
        if (request.minPrice.HasValue)
        {
            conditions.Add("p.price_amount >= @MinPrice");
        }
        if (request.maxPrice.HasValue)
        {
            conditions.Add("p.price_amount <= @MaxPrice");
        }
        if (!string.IsNullOrEmpty(request.keyword))
        {
            conditions.Add("p.name LIKE '%' || @Keyword || '%'");
        }

        // Add conditions if there are any
        if (conditions.Any())
        {
            sqlBuilder.Append(" WHERE " + string.Join(" AND ", conditions));
        }

        // Handle sorting
        var allowedSortColumns = new[] { "Name", "PriceAmount", "CreatedOn" }; // Add all allowed columns here
        var sortColumn = allowedSortColumns.Contains(request.sortColumn) ? request.sortColumn : "CreatedOn"; // Default to CreatedOn if invalid
        var sortOrder = request.sortOrder?.ToLower() == "desc" ? "DESC" : "ASC"; // Default to ASC if invalid

        sqlBuilder.Append($" ORDER BY {sortColumn} {sortOrder}");

        // Add pagination
        sqlBuilder.Append(" OFFSET (@PageSize * (@Page - 1)) ROWS FETCH NEXT @PageSize ROWS ONLY;");

        var sql = sqlBuilder.ToString();

        int page = request.page > 0 ? request.page : 1;
        int pageSize = request.pageSize > 0 ? request.pageSize : 10;

        // Execute query with parameters
        var products = await connection.QueryAsync<ProductResponse>(sql, new
        {
            ProductCategoryId = request.productCategoryId,
            MinPrice = request.minPrice,
            MaxPrice = request.maxPrice,
            Keyword = request.keyword,
            Page = page,
            PageSize = pageSize
        });

        var totalRecords = products.FirstOrDefault()?.TotalRecords ?? 0;

        await _cacheService.SetAsync(cacheKey, products, cancellationToken);

        var pagedProducts = await PagedList<ProductResponse>.CreateAsync(products, page, pageSize, totalRecords);

        return pagedProducts;
    }


    private string GenerateCacheKey(SearchProductsQuery request)
    {
        // Create an anonymous object with all the necessary parameters
        var parameters = new
        {
            request.page,
            request.pageSize,
            request.productCategoryId,
            request.minPrice,
            request.maxPrice,
            request.keyword,
            request.sortOrder,
            request.sortColumn
        };

        // Serialize to JSON
        var serializedParams = JsonSerializer.Serialize(parameters);

        // Hash the serialized string (SHA256 example)
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(serializedParams));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
