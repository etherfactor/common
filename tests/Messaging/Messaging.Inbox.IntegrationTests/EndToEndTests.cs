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
}
