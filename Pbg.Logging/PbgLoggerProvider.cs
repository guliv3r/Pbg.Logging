using Microsoft.Extensions.Logging;
using Pbg.Logging.Model;
using System.Threading.Channels;

namespace Pbg.Logging;

[ProviderAlias("Pbg")]
internal class PbgLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ChannelWriter<PbgLogEntry> _writer;
    private IExternalScopeProvider? _scopeProvider;

    public PbgLoggerProvider(Channel<PbgLogEntry> channel)
    {
        _writer = channel.Writer;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new PbgLogger(_writer, _scopeProvider);
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public void Dispose() { }
}