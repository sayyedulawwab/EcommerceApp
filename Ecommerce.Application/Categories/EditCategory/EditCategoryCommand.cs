using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Categories.EditCategory;
public record EditCategoryCommand(Guid Id, string Name, string Code) : ICommand<Guid>;
