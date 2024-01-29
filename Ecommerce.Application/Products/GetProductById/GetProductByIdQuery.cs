using Ecommerce.Application.Abstractions.Messaging;
namespace Ecommerce.Application.Products.GetProductById;

public record GetProductByIdQuery(Guid id) : IQuery<ProductResponse>;