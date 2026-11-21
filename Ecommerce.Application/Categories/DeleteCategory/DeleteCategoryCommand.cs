using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Categories.DeleteCategory;
public record DeleteCategoryCommand(Guid Id) : ICommand<Guid>;
