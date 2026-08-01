namespace EtherGizmos.Common.Abstractions;

public interface IMessageBusRegistry
{
    Task OnReady { get; }

    void MarkReady();

    bool TryGetBusId(
        string logicalName,
        out string? busId);

    bool TryGetBus(
        string busId,
        out IMessageBus? bus);

    void RegisterListener(
        string busId,
        string logicalName);

    void RegisterPublisher(
        string busId,
        string logicalName);

    void UnregisterListener(
        string logicalName);

    void UnregisterPublisher(
        string logicalName);
}
