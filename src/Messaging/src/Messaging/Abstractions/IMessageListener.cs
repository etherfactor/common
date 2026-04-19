using System.Threading.Channels;

namespace EtherGizmos.Common.Abstractions;

public interface IMessageListener : IDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    ChannelReader<ReceivedMessage> Channel { get; }
}
