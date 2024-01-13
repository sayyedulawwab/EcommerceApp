using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.ProductCategories.GetAllProductCategories;
public record GetAllProductCategoriesQuery() : IQuery<IReadOnlyList<ProductCategoryResponse>>;
