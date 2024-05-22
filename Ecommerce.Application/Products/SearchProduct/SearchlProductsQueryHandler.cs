using Dapper;
using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;

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

        IEnumerable<ProductResponse>? cachedProductResponses = await _cacheService.GetAsync<IEnumerable<ProductResponse>>($"products-{request.page}", cancellationToken);


        if (cachedProductResponses is not null)
        {
            var totalRecordsFromCache = cachedProductResponses.FirstOrDefault()?.TotalRecords ?? 0;
            var pagedProductsFromCache = await PagedList<ProductResponse>.CreateAsync(cachedProductResponses, request.page, request.pageSize, totalRecordsFromCache);
            return pagedProductsFromCache;
        }

        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = """
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
                
            FROM products AS p
            WHERE 
                (@ProductName is null or p.name LIKE @ProductName)
            ORDER BY 
                created_on
            OFFSET 
                (@PageSize * (@Page - 1)) ROWS
            FETCH NEXT 
                @PageSize ROWS ONLY;
            """;

        var products = await connection.QueryAsync<ProductResponse>(sql, new { ProductName = request.keyword, Page = request.page, PageSize = request.pageSize});

        var totalRecords = products.FirstOrDefault()?.TotalRecords ?? 0;

        await _cacheService.SetAsync($"products-{request.page}", products, cancellationToken);

        var pagedProducts = await PagedList<ProductResponse>.CreateAsync(products, request.page, request.pageSize, totalRecords);

        return pagedProducts;
    }
}
