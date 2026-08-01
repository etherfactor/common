using System.Threading.Channels;

namespace EtherGizmos.Common.Abstractions;

public interface IMessageListenerTransport
{
    Task StartAsync(
        ChannelWriter<ReceivedMessage> output,
        CancellationToken cancellationToken = default);

    Task StopReceivingAsync(
        CancellationToken cancellationToken = default);

    Task StopAsync(
        CancellationToken cancellationToken = default);
}
