using Microsoft.Extensions.Logging;
using Pbg.Logging.Model;
using System.Diagnostics;
using System.Threading.Channels;

namespace Pbg.Logging;

internal class PbgLogger : ILogger
{
    private readonly ChannelWriter<PbgLogEntry> _writer;
    private readonly IExternalScopeProvider? _scopeProvider;

    public PbgLogger(ChannelWriter<PbgLogEntry> writer, IExternalScopeProvider? scopeProvider)
    {
        _writer = writer;
        _scopeProvider = scopeProvider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _scopeProvider?.Push(state);

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsLevelEnabled(logLevel)) return;

        var entry = new PbgLogEntry
        {
            Timestamp = DateTime.UtcNow,
            LogLevel = logLevel.ToString(),
            Message = formatter(state, exception),
            Exception = exception?.ToString(),
        };

        string? foundTraceId = Activity.Current?.TraceId.ToHexString();
        string? foundUserId = null;

        _scopeProvider?.ForEachScope<object?>((scope, stateRef) =>
        {
            if (scope is IEnumerable<KeyValuePair<string, object>> properties)
            {
                foreach (var prop in properties)
                {
                    if (prop.Key == "UserId") foundUserId = prop.Value?.ToString();
                    if (prop.Key == "TraceId" && string.IsNullOrEmpty(foundTraceId))
                        foundTraceId = prop.Value?.ToString();
                }
            }
        }, null);

        entry.UserId = foundUserId;

        entry.TraceId = foundTraceId ?? Guid.NewGuid().ToString("N");

        _writer.TryWrite(entry);
    }

    private bool IsLevelEnabled(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => true,
            LogLevel.Information => true,
            LogLevel.Warning => true,
            LogLevel.Error => true,
            LogLevel.Critical => true,
            _ => false
        };
    }
}