using EtherGizmos.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notifications.Test;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddSerilog((services, logger) =>
    logger.ReadFrom.Configuration(services.GetRequiredService<IConfiguration>()),
    writeToProviders: true);

builder.Services.Add

builder.Services
    .AddNotifications("RabbitMQ", opt =>
    {
        opt.AddNotification<TestDomainEvent>("test.domain.event", type =>
        {

        });
    });

var app = builder.Build();

app.Run();
