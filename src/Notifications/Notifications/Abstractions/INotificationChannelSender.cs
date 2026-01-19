namespace EtherGizmos.Common.Abstractions;

public interface INotificationChannelSender
{
    string ChannelKey { get; }

    Task SendAsync(
        INotificationEnvelope envelope,
        CancellationToken cancellationToken = default);
}
