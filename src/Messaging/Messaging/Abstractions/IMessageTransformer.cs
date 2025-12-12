namespace EtherGizmos.Common.Abstractions;

public interface IMessageTransformer
{
    Task<SentMessage> WrapAsync(SentMessage envelope, CancellationToken cancellationToken = default);

    Task<ReceivedMessage> UnwrapAsync(ReceivedMessage envelope, CancellationToken cancellationToken = default);
}
