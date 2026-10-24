using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Orders.Events;
public sealed record OrderPlacedDomainEvent(OrderId OrderId) : IDomainEvent;
