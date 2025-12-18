using EtherGizmos.Common;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EtherGizmos.Common;

internal class IChildContainerBuilderExtensionsTests
{
    private IServiceCollection _serviceCollection;

    [SetUp]
    public void SetUp()
    {
        _serviceCollection = new ServiceCollection();
    }

    [Test]
    public void ImportLogging_WithTransientService_ResolvesLogger()
    {
        //Arrange
        var logger = new TestLogger();
        _serviceCollection
            .AddSingleton<ILoggerFactory>(e => new TestLoggerFactory(logger))
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddTransient<Wrapper>();
            })
            .ImportLogging()
            .ForwardTransient<Wrapper>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var factory = provider.GetRequiredService<ILoggerFactory>();
        var wrapper = provider.GetRequiredService<Wrapper>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.Logger, Is.Not.Null);
            Assert.That(wrapper.Logger.GetType(), Is.EqualTo(typeof(LoggerForward<Wrapper>)));
        });

        Assert.Multiple(() =>
        {
            Assert.That(factory, Is.Not.Null);
            Assert.That(factory.GetType(), Is.EqualTo(typeof(TestLoggerFactory)));
        });

        Assert.That(logger.IsLogged, Is.False);
        wrapper.Logger.Log(LogLevel.Information, "");
        Assert.That(logger.IsLogged, Is.True);
    }

    [Test]
    public void ImportLogging_WithScopedService_ResolvesLogger()
    {
        //Arrange
        var logger = new TestLogger();
        _serviceCollection
            .AddSingleton<ILoggerFactory>(e => new TestLoggerFactory(logger))
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddScoped<Wrapper>();
            })
            .ImportLogging()
            .ForwardScoped<Wrapper>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var scope = provider.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var wrapper = scope.ServiceProvider.GetRequiredService<Wrapper>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.Logger, Is.Not.Null);
            Assert.That(wrapper.Logger.GetType(), Is.EqualTo(typeof(LoggerForward<Wrapper>)));
        });

        Assert.Multiple(() =>
        {
            Assert.That(factory, Is.Not.Null);
            Assert.That(factory.GetType(), Is.EqualTo(typeof(TestLoggerFactory)));
        });

        Assert.Multiple(() =>
        {
            Assert.That(logger.IsLogged, Is.False);
            wrapper.Logger.Log(LogLevel.Information, "");
            Assert.That(logger.IsLogged, Is.True);
        });
    }

    private class TestLoggerFactory : ILoggerFactory
    {
        public ILogger Logger { get; }

        public TestLoggerFactory(ILogger logger)
        {
            Logger = logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return Logger;
        }

        public void Dispose()
        {
        }
    }

    private class TestLogger : ILogger
    {
        public bool IsLogged { get; private set; } = false;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null!;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            IsLogged = true;
        }
    }

    private class Wrapper
    {
        public ILogger Logger { get; }

        public Wrapper(ILogger<Wrapper> logger)
        {
            Logger = logger;
        }
    }
}
