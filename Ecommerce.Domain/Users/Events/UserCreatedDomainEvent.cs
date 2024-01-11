using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Users.Events;
public sealed record UserCreatedDomainEvent(Guid UserId) : IDomainEvent;
