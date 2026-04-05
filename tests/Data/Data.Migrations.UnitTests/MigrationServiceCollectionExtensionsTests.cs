using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

internal class MigrationServiceCollectionExtensionsTests
{
    [Test]
    public void AddMigrations_WithoutMigrationId_ShouldReturnBuilderWithEmptyId()
    {
        var services = new ServiceCollection();
        var assemblies = new[] { typeof(MigrationServiceCollectionExtensionsTests).Assembly };

        var builder = services.AddMigrations(assemblies);

        Assert.That(builder, Is.Not.Null);
        Assert.That(builder.MigrationId, Is.EqualTo(string.Empty));
        Assert.That(builder.Assemblies, Is.EqualTo(assemblies));
        Assert.That(builder.Services, Is.SameAs(services));
    }

    [Test]
    public void AddMigrations_WithMigrationId_ShouldReturnBuilderWithExpectedValues()
    {
        var services = new ServiceCollection();
        var assemblies = new[] { typeof(MigrationServiceCollectionExtensionsTests).Assembly };

        var builder = services.AddMigrations("main", assemblies);

        Assert.That(builder, Is.Not.Null);
        Assert.That(builder.MigrationId, Is.EqualTo("main"));
        Assert.That(builder.Assemblies, Is.EqualTo(assemblies));
        Assert.That(builder.Services, Is.SameAs(services));
    }
}
