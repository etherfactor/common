using EtherGizmos.Common.Abstractions;
using System.Threading.Channels;

namespace EtherGizmos.Common.Services;

internal class OutboxSignal : IOutboxSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite,
        });

    public void Pulse()
        => _channel.Writer.TryWrite(true);

    public async Task WaitAsync(CancellationToken cancellationToken)
        => await _channel.Reader.ReadAsync(cancellationToken);
}
