using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Products.SearchProduct;
public record SearchProductsQuery(string name) : IQuery<IReadOnlyList<ProductResponse>>;
