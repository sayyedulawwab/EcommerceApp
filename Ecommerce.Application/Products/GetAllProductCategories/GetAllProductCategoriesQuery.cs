using Ecommerce.Application.Abstactions.Messaging;

namespace Ecommerce.Application.Products.GetAllProductCategories;
public record GetAllProductCategoriesQuery() : IQuery<IReadOnlyList<ProductCategoryResponse>>;
