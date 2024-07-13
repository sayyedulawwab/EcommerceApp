using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Products.SearchProduct;
public record SearchProductsQuery(string? keyword, int page = 1, int pageSize = 10) : IQuery<PagedList<ProductResponse>>;
