using Ecommerce.Domain.Abstractions;

namespace Ecommerce.Domain.Reviews.Events;

public sealed record ReviewCreatedDomainEvent(ReviewId ReviewId) : IDomainEvent;
