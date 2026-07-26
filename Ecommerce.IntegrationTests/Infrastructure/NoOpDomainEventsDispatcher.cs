using Ecommerce.Domain.Abstractions;
using Ecommerce.Infrastructure.DomainEvents;

namespace Ecommerce.IntegrationTests.Infrastructure;

internal sealed class NoOpDomainEventsDispatcher
    : IDomainEventsDispatcher
{
    public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}