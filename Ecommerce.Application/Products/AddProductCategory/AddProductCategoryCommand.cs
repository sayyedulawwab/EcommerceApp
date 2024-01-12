using Ecommerce.Application.Abstactions.Messaging;

namespace Ecommerce.Application.Products.AddProductCategory;
public record AddProductCategoryCommand(string name, string code) : ICommand<Guid>;
