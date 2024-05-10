using Dapper;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Application.Products.SearchProduct;
internal sealed class SearchlProductsQueryHandler : IQueryHandler<SearchProductsQuery, IReadOnlyList<ProductResponse>>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    public SearchlProductsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }
    public async Task<Result<IReadOnlyList<ProductResponse>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
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
                p.product_category_id AS ProductCategoryId

            FROM products AS p
            WHERE (@ProductName IS NULL OR p.name LIKE @ProductName)
            """;

        var products = await connection
            .QueryAsync<ProductResponse>(sql, new { ProductName = request.name });

        return products.ToList();
    }
}
