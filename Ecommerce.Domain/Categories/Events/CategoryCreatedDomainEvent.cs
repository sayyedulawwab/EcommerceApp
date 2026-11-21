using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Categories.Events;
public sealed record CategoryCreatedDomainEvent(CategoryId CategoryId) : IDomainEvent;
