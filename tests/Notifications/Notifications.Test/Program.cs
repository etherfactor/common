using EtherGizmos.Common;
using EtherGizmos.Common.Abstractions;
using Microsoft.AspNetCore.HttpOverrides;
using Notifications.Test;
using Serilog;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile($"appsettings.{builder.Environment}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddRemappedEnvironmentVariables(
        new Remap(new(@"(?<=[^:_])_(?=[^_])"), ".")!,
        new Remap(new(@"(?<=[^_]):_(?=[^_])"), " ")!,
        new Remap(new(@"^ConnectionStrings:(?=[^_:])"), "")!);
//.AddExpandedConnections(builder.Configuration);

builder.Logging.ClearProviders();

builder.Services.AddSerilog((services, logger) =>
    logger.ReadFrom.Configuration(services.GetRequiredService<IConfiguration>()),
    writeToProviders: true);

var rmq = new RabbitMqBuilder("rabbitmq:latest").Build();
await rmq.StartAsync();

var psql = new PostgreSqlBuilder("postgres:latest").Build();
await psql.StartAsync();

builder.Configuration
    .AddInMemoryCollection(new Dictionary<string, string?>()
    {
        ["Connections:GeneralDatabase:Type"] = "Database",
        ["Connections:GeneralDatabase:PostgreSql:ConnectionString"] = $"{psql.GetConnectionString()}; Include Error Detail=true;",
        ["Connections:NotificationBus:Type"] = "MessageBroker",
        ["Connections:NotificationBus:RabbitMQ:ConnectionString"] = $"{rmq.GetConnectionString()}",
    });

builder.Services
    .AddConnectionResolver()
    .WithRabbitMQ()
    .WithPostgreSql();

//builder.Services
//    .AddMessaging("Messaging", (opt, conf) => { })
//    .UseConnection("NotificationBus");

builder.Services
    .AddNotifications("GeneralDatabase", "NotificationBus", opt =>
    {
        opt.AddNotification<TestDomainEvent, TestDomainEventRouter>("test.domain.event", type =>
        {
            type.Supports<TestDomainEvent, WebhookMethod, TestDomainEventWebhookFormatter>();
            type.SupportsDigest<TestDomainEvent, WebhookMethod, TestDomainEventDigestWebhookFormatter>();
        });

        opt.AddWebhookChannel();
    });

builder.Services.AddHostedService<EventHostedService>();

// Controllers
builder.Services
    .AddRouting(opt =>
    {
        opt.LowercaseUrls = true;
    })
    .AddControllers();

var app = builder.Build();

app.UseForwardedHeaders(
    new()
    {
        ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedHost
    });

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app
    .UseCors(opt =>
    {
        opt.AllowAnyOrigin();
        opt.AllowAnyMethod();
        opt.AllowAnyHeader();
    });

app
    .Use(async (context, next) =>
    {
        context.Request.EnableBuffering();
        await next();
    });

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
