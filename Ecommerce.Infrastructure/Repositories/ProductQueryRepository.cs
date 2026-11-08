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
    public async Task<ProductSearchResult> SearchAsync(
        SearchProductQuery query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = sqlConnectionFactory.CreateConnection();

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
            // The keyword is text to find, not a pattern: % and _ are escaped in the parameter below.
            conditions.Add("p.name ILIKE '%' || @Keyword || '%' ESCAPE '\\'");
        }

        // The page query and the count query share this WHERE clause, so the total always matches the filters.
        string where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;

        // Handle sorting
        // Column names cannot be parameters, so only these values are ever appended to the SQL.
        string[] allowedSortColumns = ["Name", "PriceAmount", "CreatedOnUtc"];
        string sortColumn = query.SortColumn is { } column && allowedSortColumns.Contains(column) ? column : "CreatedOnUtc"; // Default to CreatedOnUtc if invalid
        string sortOrder = query.SortOrder?.ToLower(CultureInfo.CurrentCulture) == "desc" ? "DESC" : "ASC"; // Default to ASC if invalid

        // Two statements in one round trip: the page of rows, then the number of rows that match the whole search.
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
            p.category_id AS CategoryId
        FROM products AS p");

        sqlBuilder.Append(where);

        // The ID is the last sort key, so rows with equal values keep one order and a page never repeats or skips a row.
        sqlBuilder.AppendFormat(CultureInfo.InvariantCulture, " ORDER BY {0} {1}, p.id", sortColumn, sortOrder);

        sqlBuilder.Append(" OFFSET (@PageSize * (@Page - 1)) ROWS FETCH NEXT @PageSize ROWS ONLY;");

        sqlBuilder.Append(" SELECT COUNT(*) FROM products AS p").Append(where).Append(';');

        string sql = sqlBuilder.ToString();

        // Execute query with parameters
        CommandDefinition command = new(sql, new
        {
            query.CategoryId,
            query.MinPrice,
            query.MaxPrice,
            Keyword = EscapeLikePattern(query.Keyword),
            Page = page,
            PageSize = pageSize
        }, cancellationToken: cancellationToken);

        await using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(command);

        IEnumerable<ProductResponse> products = await grid.ReadAsync<ProductResponse>();
        long totalCount = await grid.ReadSingleAsync<long>();

        return new ProductSearchResult([.. products], totalCount);
    }

    // ILIKE treats % and _ as wildcards and \ as the escape character, so each one gets a \ in front of it.
    private static string? EscapeLikePattern(string? value) =>
        value?.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
