using Ecommerce.Application.Products.SearchProduct;

namespace Ecommerce.Application.Products;

// Product reads written in SQL with Dapper. The implementation lives in Infrastructure.
public interface IProductQueryRepository
{
    Task<IReadOnlyList<ProductResponse>> SearchAsync(
        SearchProductQuery query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
