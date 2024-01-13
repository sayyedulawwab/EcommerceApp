using Dapper;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;

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
                p.PriceAmount,
                p.PriceCurrency,
                p.Quantity,
                p.CreatedOn,
                p.UpdatedOn,
                p.ProductCategoryId,

            FROM products AS p
            WHERE p.Name is LIKE '%@ProductName%'
            """;

        var products = await connection
            .QueryAsync<ProductResponse>(sql, new { request.name });

        return products.ToList();
    }
}
