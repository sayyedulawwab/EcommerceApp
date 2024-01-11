using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.ProductCategories.Events;
public sealed record ProductCategoryCreatedDomainEvent(Guid UserId) : IDomainEvent;
