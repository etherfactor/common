namespace EtherGizmos.Common.Abstractions;

public interface IOutboxSignal
{
    void Pulse();

    Task WaitAsync(CancellationToken cancellationToken = default);
}
