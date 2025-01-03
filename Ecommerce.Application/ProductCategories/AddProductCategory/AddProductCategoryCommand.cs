using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.ProductCategories.AddProductCategory;
public record AddProductCategoryCommand(string Name, string Code) : ICommand<Guid>;
