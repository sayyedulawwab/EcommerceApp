using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Categories.AddCategory;
public record AddCategoryCommand(string Name, string Code) : ICommand<Guid>;
