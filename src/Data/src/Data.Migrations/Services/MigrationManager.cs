using EtherGizmos.Common.Abstractions;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class MigrationManager : IMigrationManager
{
    private readonly IServiceProvider _serviceProvider;

    private bool _isMigrated;
    private readonly object _lock = new();

    public MigrationManager(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task EnsureMigratedAsync(
        CancellationToken cancellationToken = default)
    {
        if (_isMigrated)
            return Task.CompletedTask;

        lock (_lock)
        {
            if (_isMigrated)
                return Task.CompletedTask;

            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider;

            var runner = provider.GetRequiredService<IMigrationRunner>();

            runner.MigrateUp();
            _isMigrated = true;
        }

        return Task.CompletedTask;
    }
}
