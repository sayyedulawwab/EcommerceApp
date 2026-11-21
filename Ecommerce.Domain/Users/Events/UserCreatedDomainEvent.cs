using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Users.Events;
public sealed record UserCreatedDomainEvent(UserId UserId) : IDomainEvent;
