namespace EtherGizmos.Common.Abstractions;

public interface IMigrationManager
{
    Task EnsureMigratedAsync(CancellationToken cancellationToken = default);
}
