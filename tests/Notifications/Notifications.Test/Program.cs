using EtherGizmos.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notifications.Test;
using Serilog;
using Testcontainers.RabbitMq;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile($"appsettings.{builder.Environment}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddRemappedEnvironmentVariables(
        new Remap(new(@"(?<=[^:_])_(?=[^_])"), "."),
        new Remap(new(@"(?<=[^_]):_(?=[^_])"), " "),
        new Remap(new(@"^ConnectionStrings:(?=[^_:])"), ""));
    //.AddExpandedConnections(builder.Configuration);

builder.Logging.ClearProviders();

builder.Services.AddSerilog((services, logger) =>
    logger.ReadFrom.Configuration(services.GetRequiredService<IConfiguration>()),
    writeToProviders: true);

var rmq = new RabbitMqBuilder("rabbitmq:latest").Build();
await rmq.StartAsync();

builder.Configuration
    .AddInMemoryCollection(new Dictionary<string, string?>()
    {
        ["Connections:NotificationBus:Type"] = "MessageBroker",
        ["Connections:NotificationBus:RabbitMQ:ConnectionString"] = rmq.GetConnectionString(),
    });

builder.Services
    .AddConnectionResolver()
    .WithRabbitMQ();

builder.Services
    .AddMessaging("Messaging", (opt, conf) => { })
    .UseConnection("NotificationBus");

builder.Services
    .AddNotifications("Messaging", opt =>
    {
        opt.AddNotification<TestDomainEvent>("test.domain.event", type =>
        {

        });
    });

builder.Services.AddHostedService<EventHostedService>();

var app = builder.Build();

app.Run();
