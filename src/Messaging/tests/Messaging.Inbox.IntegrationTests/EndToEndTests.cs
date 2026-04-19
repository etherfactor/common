using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace EtherGizmos.Common;

internal class EndToEndTests : IntegrationTestBase
{
    [SetUp]
    public async Task SetUp()
    {
        TestConsumer.Reset();
    }

    [Test]
    public async Task Queue_WithOneConsumer_ShouldConsume()
    {
        //Arrange
        var q = "test.queue." + Guid.NewGuid().ToString("N");

        using var host = await BuildHostAsync(opt =>
        {
            opt.Listeners.AddQueue("q", q);
            opt.Publishers.AddQueue("q", q);
        },
        consumersAssemblies: [typeof(TestConsumer).Assembly]);

        var sender = host.Services.GetRequiredService<IMessageSender>();

        //Act
        await sender.SendAsync("q", new TestMessage { Value = "hello" });

        var consumed = await TestConsumer.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        //Assert
        Assert.That(consumed.Value, Is.EqualTo("hello"));
        await host.StopAsync();
    }

    [Test]
    public async Task Queue_WithTwoConsumers_ShouldRetryOnlyFailed()
    {
        //Arrange
        var q = "test.queue." + Guid.NewGuid().ToString("N");

        using var host = await BuildHostAsync(opt =>
        {
            opt.Listeners.AddQueue("q", q);
            opt.Publishers.AddQueue("q", q);
        },
        consumersAssemblies: [typeof(SplitConsumerA).Assembly]);

        var sender = host.Services.GetRequiredService<IMessageSender>();

        //Act
        await sender.SendAsync("q", new SplitMessage { Value = "hello" });

        var consumedA = await SplitConsumerA.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var consumedB = await SplitConsumerB.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(consumedA.Value, Is.EqualTo("hello"));
            Assert.That(SplitConsumerA.InvocationCount, Is.EqualTo(1));
            Assert.That(SplitConsumerB.InvocationCount, Is.EqualTo(2));
        }
        await host.StopAsync();
    }

    private async Task<IHost> BuildHostAsync(
        Action<MessagingOptions> configureMessaging,
        Assembly[]? consumersAssemblies = null)
    {
        consumersAssemblies ??= new[] { typeof(TestConsumer).Assembly };

        var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                var configuration = new ConfigurationManager();
                services.AddSingleton<IConfiguration>(configuration);

                configuration.AddInMemoryCollection(new Dictionary<string, string?>()
                {
                    ["Connections:Rabbit:Type"] = "MessageBroker",
                    ["Connections:Rabbit:RabbitMQ:ConnectionString"] = Setup.RmqConnectionString,
                    ["Connections:Postgres:Type"] = "Database",
                    ["Connections:Postgres:PostgreSql:ConnectionString"] = Setup.PgSqlConnectionString,
                });

                services.AddConnectionResolver()
                    .WithRabbitMQ()
                    .WithPostgreSql();

                services
                    .AddMessaging("bus1", (opt, _) =>
                    {
                        configureMessaging(opt);
                    })
                    .UseConnection("Rabbit")
                    .AddConsumersFromAssemblies(consumersAssemblies)
                    .UseInbox("Postgres");
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    public sealed class TestMessage
    {
        public string Value { get; init; } = null!;
    }

    internal sealed class TestConsumer : IMessageConsumer<TestMessage>
    {
        public static TaskCompletionSource<TestMessage> Tcs { get; private set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static void Reset() =>
            Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ConsumeAsync(
            IMessageContext<TestMessage> context)
        {
            Tcs.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    public sealed class SplitMessage
    {
        public string Value { get; init; } = null!;
    }

    internal sealed class SplitConsumerA : IMessageConsumer<SplitMessage>
    {
        public static TaskCompletionSource<SplitMessage> Tcs { get; private set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static int InvocationCount => _invocations;
        private static int _invocations;

        public static void Reset() =>
            Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ConsumeAsync(
            IMessageContext<SplitMessage> context)
        {
            Interlocked.Increment(ref _invocations);

            Tcs.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    internal sealed class SplitConsumerB : IMessageConsumer<SplitMessage>
    {
        public static TaskCompletionSource<SplitMessage> Tcs { get; private set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static int InvocationCount => _invocations;
        private static int _invocations;

        public static void Reset()
        {
            Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _invocations = 0;
        }

        public async Task ConsumeAsync(IMessageContext<SplitMessage> context)
        {
            var attempt = Interlocked.Increment(ref _invocations);

            if (attempt == 1)
            {
                await context.Actions.AbandonAsync(context.CancellationToken);
                return;
            }

            // second delivery (or later): succeed
            Tcs.TrySetResult(context.Message);
        }
    }
}
