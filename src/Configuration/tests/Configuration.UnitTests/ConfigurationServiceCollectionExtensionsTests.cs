using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

internal class ConfigurationServiceCollectionExtensionsTests
{
    [Test]
    public void AddConnectionResolver_WhenCalled_ShouldAddResolver()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        var builder = services.AddConnectionResolver();

        using var provider = services.BuildServiceProvider();
        var resolver = provider.GetService<IConnectionResolver>();

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolver, Is.Not.Null);
            Assert.That(builder, Is.AssignableTo<IConnectionResolverBuilder>());
        }
    }

    [Test]
    public void AddKeyResolver_WhenCalled_ShouldAddResolver()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        var builder = services.AddKeyResolver();

        using var provider = services.BuildServiceProvider();
        var resolver = provider.GetService<IKeyResolver>();

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolver, Is.Not.Null);
            Assert.That(builder, Is.AssignableTo<IKeyResolverBuilder>());
        }
    }
}
