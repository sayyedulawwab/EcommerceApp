using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Products.SearchProduct;
public record SearchProductsQuery(Guid? productCategoryId,
                                    decimal? minPrice,
                                    decimal? maxPrice,
                                    string? keyword,
                                    int page,
                                    int pageSize,
                                    string? sortColumn,
                                    string? sortOrder) : IQuery<PagedList<ProductResponse>>;
