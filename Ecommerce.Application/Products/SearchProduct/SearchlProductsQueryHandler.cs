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
                p.Id,
                p.Name,
                p.Description,
                p.Price_Amount AS PriceAmount,
                p.Price_Currency AS PriceCurrency,
                p.Quantity,
                p.CreatedOn,
                p.UpdatedOn,
                p.ProductCategoryId

            FROM Products AS p
            WHERE (@ProductName IS NULL OR p.Name LIKE @ProductName)
            """;

        var products = await connection
            .QueryAsync<ProductResponse>(sql, new { ProductName = request.name });

        return products.ToList();
    }
}
