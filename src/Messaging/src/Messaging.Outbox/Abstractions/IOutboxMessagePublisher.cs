
namespace EtherGizmos.Common.Abstractions;

public interface IOutboxMessagePublisher
{
    Task<bool> PublishAsync(CancellationToken cancellationToken = default);
}
