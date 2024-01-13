using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.ProductCategories.EditProductCategory;
public record EditProductCategoryCommand(Guid id, string name, string code) : ICommand<Guid>;
