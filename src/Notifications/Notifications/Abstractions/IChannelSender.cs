namespace EtherGizmos.Common.Abstractions;

public interface IChannelSender<in TEnvelope>
    where TEnvelope : IChannelEnvelope
{
    Task SendAsync(
        TEnvelope envelope,
        CancellationToken cancellationToken = default);
}
