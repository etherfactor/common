using System.Threading.Channels;

namespace EtherGizmos.Common.Abstractions;

public interface IMessagePublisher : IDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    ChannelWriter<SentMessage> Channel { get; }
}
