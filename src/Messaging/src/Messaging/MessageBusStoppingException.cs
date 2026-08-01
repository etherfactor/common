namespace EtherGizmos.Common;

public sealed class MessageBusStoppingException : InvalidOperationException
{
    public MessageBusStoppingException(string logicalName)
        : base($"The message destination '{logicalName}' is stopping and no longer accepts messages.")
    {
    }
}
