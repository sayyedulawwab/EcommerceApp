using Ecommerce.Application.Abstractions.Messaging;
namespace Ecommerce.Application.ProductCategories.GetProductCategoryById;

public record GetProductCategoryByIdQuery(Guid Id) : IQuery<ProductCategoryResponse>;
