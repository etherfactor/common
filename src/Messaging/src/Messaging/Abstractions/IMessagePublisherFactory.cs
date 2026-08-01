namespace EtherGizmos.Common.Abstractions;

public interface IMessagePublisherFactory
{
    IMessagePublisherTransport CreatePublisherForQueue(
        string logicalName,
        string queue);

    IMessagePublisherTransport CreatePublisherForTopic(
        string logicalName,
        string topic);
}
