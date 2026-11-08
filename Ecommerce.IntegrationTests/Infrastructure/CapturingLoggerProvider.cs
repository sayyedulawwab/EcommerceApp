using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Ecommerce.IntegrationTests.Infrastructure;

// Keeps every log entry written by the host, so a test can assert on what the app logged.
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new CapturingLogger(Entries);
    }

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<(LogLevel Level, string Message)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue((logLevel, formatter(state, exception)));
        }
    }
}
