using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EtherGizmos.Common.Services;

public class MigrationManagerTests
{
    [Test]
    public async Task EnsureMigratedAsync_WhenCalled_ShouldCallRunnerOnce()
    {
        var runner = new Mock<IMigrationRunner>();

        var services = new ServiceCollection();
        services.AddKeyedScoped("key", (_, _) => runner.Object);

        var provider = services.BuildServiceProvider();
        var manager = new MigrationManager("key", provider);

        await manager.EnsureMigratedAsync();
        await manager.EnsureMigratedAsync();

        runner.Verify(x => x.MigrateUp(), Times.Once);
    }

    [Test]
    public async Task EnsureMigratedAsync_WhenCalledConcurrently_ShouldCallRunnerOnlyOnce()
    {
        var runner = new Mock<IMigrationRunner>();

        var services = new ServiceCollection();
        services.AddKeyedScoped("key", (_, _) => runner.Object);

        var provider = services.BuildServiceProvider();
        var manager = new MigrationManager("key", provider);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => manager.EnsureMigratedAsync()))
            .ToArray();

        await Task.WhenAll(tasks);

        runner.Verify(x => x.MigrateUp(), Times.Once);
    }

    [Test]
    public void EnsureMigratedAsync_WhenRunnerMissing_ShouldThrowInvalidOperationException()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var manager = new MigrationManager("key", provider);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await manager.EnsureMigratedAsync());
    }
}
