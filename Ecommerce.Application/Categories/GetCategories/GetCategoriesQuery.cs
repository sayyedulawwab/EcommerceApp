using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Categories;

namespace Ecommerce.Application.Categories.GetCategories;
public record GetCategoriesQuery() : IQuery<IReadOnlyList<CategoryResponse>>;
