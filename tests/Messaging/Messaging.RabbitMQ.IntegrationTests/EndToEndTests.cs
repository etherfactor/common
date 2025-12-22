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
        TestConsumerA.Reset();
        TestConsumerB.Reset();

        AbandonOnceConsumer.Reset();

        DeadLetterConsumer.Reset();
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
        consumersAssemblies: [typeof(TestConsumerA).Assembly]);

        var sender = host.Services.GetRequiredService<IMessageSender>();

        //Act
        await sender.SendAsync("q", new TestMessage { Value = "hello" });

        var consumed = await TestConsumerA.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        //Assert
        Assert.That(consumed.Value, Is.EqualTo("hello"));
        await host.StopAsync();
    }

    [Test]
    public async Task Queue_WithMultipleConsumers_ShouldConsumeAll()
    {
        //Arrange
        var q = "test.queue." + Guid.NewGuid().ToString("N");

        using var host = await BuildHostAsync(opt =>
        {
            opt.Listeners.AddQueue("q", q);
            opt.Publishers.AddQueue("q", q);
        },
        consumersAssemblies: [typeof(TestConsumerA).Assembly]);

        var sender = host.Services.GetRequiredService<IMessageSender>();

        //Act
        await sender.SendAsync("q", new TestMessage { Value = "hello" });

        var completed = await Task.WhenAll(
            TestConsumerA.Tcs.Task,
            TestConsumerB.Tcs.Task).WaitAsync(TimeSpan.FromSeconds(10));

        var a = TestConsumerA.Tcs.Task.IsCompleted;
        var b = TestConsumerB.Tcs.Task.IsCompleted;

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(a, Is.True);
            Assert.That(b, Is.True);
        }
        await host.StopAsync();
    }

    [Test]
    public async Task Topic_WithMultipleSubscriptions_ShouldFanout()
    {
        //Arrange
        var topic = "test.topic." + Guid.NewGuid().ToString("N");
        var subA = "subA." + Guid.NewGuid().ToString("N");
        var subB = "subB." + Guid.NewGuid().ToString("N");

        using var host = await BuildHostAsync(opt =>
        {
            opt.Publishers.AddTopic("t", topic);

            opt.Listeners.AddTopic("t-sub-a", topic, subscription: subA);
            opt.Listeners.AddTopic("t-sub-b", topic, subscription: subB);
        },
        consumersAssemblies: [typeof(TestConsumerA).Assembly]);

        var sender = host.Services.GetRequiredService<IMessageSender>();

        //Act
        await sender.SendAsync("t", new TestMessage { Value = "ping" });

        var a = await TestConsumerA.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var b = await TestConsumerB.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(a.Value, Is.EqualTo("ping"));
            Assert.That(b.Value, Is.EqualTo("ping"));
        }
        await host.StopAsync();
    }

    [Test]
    public async Task Topic_WithRuntimeRegistration_ShouldConsumeNewMessages()
    {
        //Arrange
        var topic = "test.topic." + Guid.NewGuid().ToString("N");
        var sub = "sub." + Guid.NewGuid().ToString("N");

        using var host = await BuildHostAsync(opt =>
        {
            opt.Publishers.AddTopic("t", topic);
        },
        consumersAssemblies: [typeof(TestConsumerA).Assembly]);

        var sender = host.Services.GetRequiredService<IMessageSender>();

        await sender.SendAsync("t", new TestMessage { Value = "early" });

        var registry = host.Services.GetRequiredService<IMessageBusRegistry>();
        registry.TryGetBus("bus1", out var bus);

        Assert.That(bus, Is.Not.Null);

        await bus.RegisterListenerForTopicAsync("t", topic, sub);

        //Act
        await sender.SendAsync("t", new TestMessage { Value = "late" });

        var msg = await TestConsumerA.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        //Assert
        Assert.That(msg.Value, Is.EqualTo("late"));
        await host.StopAsync();
    }

    [Test]
    public async Task Queue_WhenAbandoned_ShouldRetryAndEventuallyConsume()
    {
        //Arrange
        var q = "test.queue." + Guid.NewGuid().ToString("N");

        using var host = await BuildHostAsync(opt =>
        {
            opt.Listeners.AddQueue("q", q);
            opt.Publishers.AddQueue("q", q);
        },
        consumersAssemblies: [typeof(AbandonOnceConsumer).Assembly]);

        var sender = host.Services.GetRequiredService<IMessageSender>();

        //Act
        await sender.SendAsync("q", new AbandonMessage { Value = "hello" });

        var consumed = await AbandonOnceConsumer.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(consumed.Value, Is.EqualTo("hello"));
            Assert.That(AbandonOnceConsumer.InvocationCount, Is.GreaterThanOrEqualTo(2),
                "Expected message to be delivered at least twice (first abandoned, then redelivered).");
        }
        await host.StopAsync();
    }

    [Test]
    public async Task Queue_WhenDeadLettered_ShouldNotRetry()
    {
        //Arrange
        var q = "test.queue." + Guid.NewGuid().ToString("N");

        using var host = await BuildHostAsync(opt =>
        {
            opt.Listeners.AddQueue("q", q);
            opt.Publishers.AddQueue("q", q);
        },
        consumersAssemblies: [typeof(DeadLetterConsumer).Assembly]);

        var sender = host.Services.GetRequiredService<IMessageSender>();

        //Act
        await sender.SendAsync("q", new DeadLetterMessage { Value = "hello" });

        await DeadLetterConsumer.FirstAttemptTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        //Assert
        Assert.ThrowsAsync<TimeoutException>(async () =>
            await DeadLetterConsumer.RedeliveredTcs.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        await host.StopAsync();
    }

    private async Task<IHost> BuildHostAsync(
        Action<MessagingOptions> configureMessaging,
        Assembly[]? consumersAssemblies = null)
    {
        consumersAssemblies ??= new[] { typeof(TestConsumerA).Assembly };

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
                });

                services.AddConnectionResolver()
                    .WithRabbitMQ();

                services
                    .AddMessaging("bus1", (opt, _) =>
                    {
                        configureMessaging(opt);
                    })
                    .UseConnection("Rabbit")
                    .AddConsumersFromAssemblies(consumersAssemblies);
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    public sealed class TestMessage
    {
        public string Value { get; init; } = null!;
    }

    internal sealed class TestConsumerA : IMessageConsumer<TestMessage>
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

    internal sealed class TestConsumerB : IMessageConsumer<TestMessage>
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

    public sealed class AbandonMessage
    {
        public string Value { get; init; } = null!;
    }

    internal sealed class AbandonOnceConsumer : IMessageConsumer<AbandonMessage>
    {
        public static TaskCompletionSource<AbandonMessage> Tcs { get; private set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static int InvocationCount => _invocations;
        private static int _invocations;

        public static void Reset()
        {
            Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _invocations = 0;
        }

        public async Task ConsumeAsync(IMessageContext<AbandonMessage> context)
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

    public sealed class DeadLetterMessage
    {
        public string Value { get; init; } = null!;
    }

    internal sealed class DeadLetterConsumer : IMessageConsumer<DeadLetterMessage>
    {
        public static TaskCompletionSource<bool> FirstAttemptTcs { get; private set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static TaskCompletionSource<bool> RedeliveredTcs { get; private set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private static int _invocations;

        public static void Reset()
        {
            FirstAttemptTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            RedeliveredTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _invocations = 0;
        }

        public async Task ConsumeAsync(IMessageContext<DeadLetterMessage> context)
        {
            var attempt = Interlocked.Increment(ref _invocations);

            if (attempt == 1)
            {
                FirstAttemptTcs.TrySetResult(true);
                await context.Actions.DeadLetterAsync(context.CancellationToken);
                return;
            }

            // If we ever see it again, that means dead-letter requeued (bad)
            RedeliveredTcs.TrySetResult(true);
        }
    }
}
