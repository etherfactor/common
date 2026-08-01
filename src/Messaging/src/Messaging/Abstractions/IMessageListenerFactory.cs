namespace EtherGizmos.Common.Abstractions;

public interface IMessageListenerFactory
{
    IMessageListenerTransport CreateListenerForQueue(
        string logicalName,
        string queue);

    IMessageListenerTransport CreateListenerForTopic(
        string logicalName,
        string topic,
        string subscription);
}
