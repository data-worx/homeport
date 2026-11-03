// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Homeport.Sdk.Tests.Integration;

/// <summary>Collects every log entry the module writes, so a test can assert on them.</summary>
internal sealed class CollectingLoggerProvider : ILoggerProvider
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WaitPoll = TimeSpan.FromMilliseconds(20);

    private readonly ConcurrentQueue<LogEntry> _entries = new();

    internal IReadOnlyList<LogEntry> Entries => [.. _entries];

    /// <summary>
    /// Waits until an entry matches <paramref name="match"/>. A background
    /// service logs on its own schedule: since .NET 10 all of its
    /// <c>ExecuteAsync</c> runs on a background thread, so the host can have
    /// started before the first line is logged.
    /// </summary>
    internal async Task<LogEntry> WaitForAsync(Func<LogEntry, bool> match)
    {
        using var timeout = new CancellationTokenSource(WaitTimeout);
        while (!_entries.Any(match))
        {
            await Task.Delay(WaitPoll, timeout.Token);
        }

        return _entries.First(match);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new CollectingLogger(categoryName, _entries);
    }

    public void Dispose()
    {
    }

    internal sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception);

    private sealed class CollectingLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception), exception));
        }
    }
}
