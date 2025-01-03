using Ecommerce.Application.Abstractions.Messaging;

namespace Ecommerce.Application.Products.DeleteProduct;
public record DeleteProductCommand(Guid Id) : ICommand<Guid>;
