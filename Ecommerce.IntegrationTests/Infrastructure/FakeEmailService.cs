using System.Collections.Concurrent;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Domain.Users;

namespace Ecommerce.IntegrationTests.Infrastructure;

// Records the emails instead of sending them, so tests can check what a domain event handler sent.
public sealed class FakeEmailService : IEmailService
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyList<SentEmail> Sent => [.. _sent];

    public Task SendAsync(Email email, string subject, string body)
    {
        _sent.Enqueue(new SentEmail(email.Value, subject, body));

        return Task.CompletedTask;
    }

    public void Clear()
    {
        _sent.Clear();
    }
}

public sealed record SentEmail(string To, string Subject, string Body);
