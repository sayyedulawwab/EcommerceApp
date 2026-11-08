namespace Ecommerce.Application.Products.SearchProduct;

// One page of products plus the number of products that match the whole search. This is what the cache stores.
public sealed record ProductSearchResult(IReadOnlyList<ProductResponse> Items, long TotalCount);
