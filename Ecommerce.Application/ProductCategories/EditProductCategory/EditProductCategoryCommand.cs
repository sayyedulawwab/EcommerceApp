using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.ProductCategories.EditProductCategory;
public record EditProductCategoryCommand(Guid Id, string Name, string Code) : ICommand<Guid>;
