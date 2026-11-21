using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Categories;
namespace Ecommerce.Application.Categories.GetCategoryById;

public record GetCategoryByIdQuery(Guid Id) : IQuery<CategoryResponse>;
