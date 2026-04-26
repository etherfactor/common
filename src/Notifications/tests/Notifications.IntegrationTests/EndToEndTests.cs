using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using EtherGizmos.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace EtherGizmos.Common;

internal class NotificationEndToEndTests : IntegrationTestBase
{
    private WebApplication _app;
    private string _baseUrl;
    private TestWebhookInbox _webhookInbox;
    private string _connectionString;

    [SetUp]
    public async Task SetUp()
    {
        _connectionString = await Setup.CreateDatabase($"z{Guid.NewGuid():N}");

        (_app, _baseUrl, _webhookInbox) = await BuildAppAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Test]
    public async Task EmitAsync_WithMatchingImmediateSubscription_ShouldDeliverWebhookAndMarkNotificationSent()
    {
        //Arrange
        await using (var scope = _app!.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();

            context.NotificationSubscriptions.Add(new NotificationSubscription
            {
                UserId = "user-1",
                EventType = "test.domain.event",
                ChannelKey = "webhook",
                ChannelConfigRaw = JsonSerializer.Serialize(new WebhookChannelConfig
                {
                    Method = "POST",
                    Endpoint = $"{_baseUrl}/test/webhooks/notifications",
                    Headers = [],
                }, JsonSerializerOptions.Web),
                ScheduleType = NotificationSchedules.Immediate.Key,
                ScheduleConfigRaw = "{}",
                IsEnabled = true,
            });

            await context.SaveChangesAsync();
        }

        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var emitter = scope.ServiceProvider.GetRequiredService<IDomainEventEmitter>();

            await emitter.EmitAsync(
                new TestDomainEvent
                {
                    Value = "hello",
                },
                [new AudienceKey("$self", "user-1")]);
        }

        //Act
        var receipt = await _webhookInbox.WaitAsync(TimeSpan.FromSeconds(15));

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.Body, Does.Contain("hello"));
            Assert.That(receipt.ContentType, Does.StartWith("application/json"));
        }

        await WaitUntilAsync(
            async () =>
            {
                await using var scope = _app.Services.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();

                return await context.Notifications
                    .Include(e => e.NotificationSubscription)
                    .AnyAsync(e =>
                        e.NotificationSubscription.UserId == "user-1"
                        && e.NotificationSubscription.ScheduleType == ImmediateSchedule.Instance.Key
                        && e.Status == NotificationStatusType.Sent);
            },
            TimeSpan.FromSeconds(15));

        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();

            var notification = await context.Notifications
                .Include(e => e.NotificationSubscription)
                .SingleAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(notification.NotificationSubscription.UserId, Is.EqualTo("user-1"));
                Assert.That(notification.NotificationSubscription.ScheduleType, Is.EqualTo(NotificationSchedules.Immediate.Key));
                Assert.That(notification.Status, Is.EqualTo(NotificationStatusType.Sent));
                Assert.That(notification.SentAt, Is.Not.Null);
                Assert.That(notification.AttemptCount, Is.GreaterThanOrEqualTo(1));
            }
        }
    }

    [Test]
    public async Task EmitAsync_WhenAudienceDoesNotMatchSubscription_ShouldNotDeliverWebhookOrCreateNotification()
    {
        //Arrange
        await using (var scope = _app!.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();

            context.NotificationSubscriptions.Add(new NotificationSubscription
            {
                UserId = "user-1",
                EventType = "test.domain.event",
                ChannelKey = "webhook",
                ChannelConfigRaw = JsonSerializer.Serialize(new WebhookChannelConfig
                {
                    Method = "POST",
                    Endpoint = $"{_baseUrl}/test/webhooks/notifications",
                    Headers = [],
                }, JsonSerializerOptions.Web),
                ScheduleType = NotificationSchedules.Immediate.Key,
                ScheduleConfigRaw = "{}",
                IsEnabled = true,
            });

            await context.SaveChangesAsync();
        }

        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var emitter = scope.ServiceProvider.GetRequiredService<IDomainEventEmitter>();

            await emitter.EmitAsync(
                new TestDomainEvent
                {
                    Value = "hello",
                },
                [new AudienceKey("$self", "someone-else")]);
        }

        //Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _webhookInbox.WaitAsync(TimeSpan.FromSeconds(3)));

        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();
            var count = await context.Notifications.CountAsync();

            Assert.That(count, Is.EqualTo(0));
        }
    }

    private async Task<(WebApplication App, string BaseUrl, TestWebhookInbox Inbox)> BuildAppAsync()
    {
        var port = GetFreeTcpPort();
        var baseUrl = $"http://127.0.0.1:{port}";

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });

        builder.WebHost.UseUrls(baseUrl);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Connections:GeneralDatabase:Type"] = "Database",
            ["Connections:GeneralDatabase:PostgreSql:ConnectionString"] = $"{_connectionString}; Include Error Detail=true;",
            ["Connections:NotificationBus:Type"] = "MessageBroker",
            ["Connections:NotificationBus:RabbitMQ:ConnectionString"] = Setup.RmqConnectionString,
        });

        builder.Services
            .AddConnectionResolver()
            .WithRabbitMQ()
            .WithPostgreSql();

        builder.Services.AddSingleton<TestWebhookInbox>();

        builder.Services
            .AddNotifications("GeneralDatabase", "NotificationBus", opt =>
            {
                opt.AddNotification<TestDomainEvent, TestDomainEventRouter>("test.domain.event", type =>
                {
                    type.HasDisplayName("Test Domain Event");
                    type.Supports<TestDomainEvent, WebhookChannel, TestDomainEventWebhookFormatter>();
                    type.SupportsDigest<TestDomainEvent, WebhookChannel, TestDomainEventDigestWebhookFormatter>();
                });

                opt.AddWebhookChannel();
            });

        var app = builder.Build();

        app.UseRouting();

        app.MapPost("/test/webhooks/notifications", async (TestWebhookInbox inbox, HttpRequest request, CancellationToken cancellationToken = default) =>
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync(cancellationToken);

            inbox.Push(new TestWebhookReceipt(
                body,
                request.ContentType));

            return Results.Ok();
        });

        await app.StartAsync();

        var inbox = app.Services.GetRequiredService<TestWebhookInbox>();
        return (app, baseUrl, inbox);
    }

    private static async Task WaitUntilAsync(
        Func<Task<bool>> predicate,
        TimeSpan timeout,
        TimeSpan? pollInterval = null)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(200);

        var started = DateTimeOffset.UtcNow;
        while (DateTimeOffset.UtcNow - started < timeout)
        {
            if (await predicate())
            {
                return;
            }

            await Task.Delay(pollInterval.Value);
        }

        throw new TimeoutException("Condition was not met within the allotted timeout.");
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    public sealed class TestDomainEvent : IDomainEvent
    {
        public string Value { get; set; } = null!;
    }

    internal sealed class TestDomainEventRouter : IDomainEventRouter<TestDomainEvent>
    {
        public async IAsyncEnumerable<string> FilterScopeAsync(
            TestDomainEvent @event,
            IEnumerable<AudienceKey> audiences,
            IEnumerable<string> userIds,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var userSet = userIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var self = audiences
                .Where(e => e.Kind == "$self")
                .Select(e => e.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            await Task.Yield();

            foreach (var userId in self)
            {
                if (userSet.Contains(userId))
                {
                    yield return userId;
                }
            }
        }
    }

    internal sealed class TestDomainEventWebhookFormatter
        : WebhookNotificationChannelFormatter<ImmediateSchedule, TestDomainEvent>;

    internal sealed class TestDomainEventDigestWebhookFormatter
        : WebhookNotificationChannelFormatter<DigestSchedule, Digest<TestDomainEvent>>;

    internal sealed class TestWebhookInbox
    {
        private readonly Channel<TestWebhookReceipt> _channel = Channel.CreateUnbounded<TestWebhookReceipt>();

        public void Push(TestWebhookReceipt receipt)
        {
            _channel.Writer.TryWrite(receipt);
        }

        public async Task<TestWebhookReceipt> WaitAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return await _channel.Reader.ReadAsync(cts.Token);
        }
    }

    internal sealed record TestWebhookReceipt(
        string Body,
        string? ContentType);
}
