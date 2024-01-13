using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.ProductCategories.AddProductCategory;
public record AddProductCommand(string name, string code) : ICommand<Guid>;
