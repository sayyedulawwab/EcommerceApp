using Dapper;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Application.Products.SearchProduct;
internal sealed class SearchlProductsQueryHandler : IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    public SearchlProductsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }
    public async Task<Result<PagedList<ProductResponse>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {

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

        var pagedProducts = await PagedList<ProductResponse>.CreateAsync(products, request.page, request.pageSize, totalRecords);
        return pagedProducts;
    }
}
