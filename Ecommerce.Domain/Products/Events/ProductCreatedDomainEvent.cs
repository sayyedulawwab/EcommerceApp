using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Products.Events;
public sealed record ProductCreatedDomainEvent(ProductId productId) : IDomainEvent;
