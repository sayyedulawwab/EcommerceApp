using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.ProductCategories.AddProductCategory;
public record AddProductCategoryCommand(string name, string code) : ICommand<Guid>;
