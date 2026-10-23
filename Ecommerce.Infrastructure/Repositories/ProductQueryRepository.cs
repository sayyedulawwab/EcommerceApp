using System.Data;
using System.Globalization;
using System.Text;
using Dapper;
using Ecommerce.Application.Products;
using Ecommerce.Application.Products.SearchProduct;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Repositories;

internal sealed class ProductQueryRepository(ISqlConnectionFactory sqlConnectionFactory) : IProductQueryRepository
{
    public async Task<IReadOnlyList<ProductResponse>> SearchAsync(
        SearchProductQuery query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
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
        if (query.CategoryId.HasValue)
        {
            conditions.Add("p.category_id = @CategoryId");
        }
        if (query.MinPrice.HasValue)
        {
            conditions.Add("p.price_amount >= @MinPrice");
        }
        if (query.MaxPrice.HasValue)
        {
            conditions.Add("p.price_amount <= @MaxPrice");
        }
        if (!string.IsNullOrEmpty(query.Keyword))
        {
            conditions.Add("p.name ILIKE '%' || @Keyword || '%'");
        }

        // Add conditions if there are any
        if (conditions.Count > 0)
        {
            sqlBuilder.Append(" WHERE " + string.Join(" AND ", conditions));
        }

        // Handle sorting
        string[] allowedSortColumns = ["Name", "PriceAmount", "CreatedOnUtc"]; // Add all allowed columns here
        string sortColumn = query.SortColumn is { } column && allowedSortColumns.Contains(column) ? column : "CreatedOnUtc"; // Default to CreatedOnUtc if invalid
        string sortOrder = query.SortOrder?.ToLower(CultureInfo.CurrentCulture) == "desc" ? "DESC" : "ASC"; // Default to ASC if invalid

        sqlBuilder.AppendFormat(CultureInfo.InvariantCulture, " ORDER BY {0} {1}", sortColumn, sortOrder);

        // Add pagination
        sqlBuilder.Append(" OFFSET (@PageSize * (@Page - 1)) ROWS FETCH NEXT @PageSize ROWS ONLY;");

        string sql = sqlBuilder.ToString();

        // Execute query with parameters
        CommandDefinition command = new(sql, new
        {
            query.CategoryId,
            query.MinPrice,
            query.MaxPrice,
            query.Keyword,
            Page = page,
            PageSize = pageSize
        }, cancellationToken: cancellationToken);

        IEnumerable<ProductResponse> products = await connection.QueryAsync<ProductResponse>(command);

        return products.ToList();
    }
}
