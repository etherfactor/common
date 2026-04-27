# EtherGizmos.Common

A suite of .NET libraries used by my other projects, centralized for convenience.

## Features

### Composition

- **Child Containers** - Support for nested child containers for complex DI scenarios

Registering child containers:
```csharp
//A bit contrived, but you can use this to register services in a child container that might not naturally allow you to
//configure dependencies. The services must be forwarded back to the primary container for them to be resolvable
builder.Services
    .AddSingleton(e => new TestA() { Data = "Test" })
    .AddChildContainer((childServices, parentServices) =>
    {
        var testA = parentServices.GetRequiredService<TestA>();
        childServices.AddSingleton(e => new TestB() { Data = testA.Data });
    })
    .ForwardSingleton<TestB>();
```

### Configuration

- **Core Configuration** - Base configuration framework
- **Data Configuration** - Configuration abstractions for data layer
- **Email Configuration** - Email service configuration and setup
- **Keys Configuration** - Secure key management and configuration
- **Messaging Configuration** - Message queue and broker configuration

Registering the connection resolver:
```csharp
builder.Services
    .AddConnectionResolver()
    .WithPostgreSql(); //Requires Data.PostgreSql package
```

Registering the key resolver:
```csharp
builder.Services
    .AddKeyResolver()
    .WithCertificates(); //Requires Security.Certificates package
```

Configuring the connection (appsettings.json):
```json
{
    "Connections": {
        "MyConnectionId": {
            "Type": "Database",
            "PostgreSql": {
                "ConnectionString": "..."
            }
        }
    },
    "Keys": {
        "MyCertificateId": {
            "Type": "Asymmetric",
            "PfxFile": {
                "Path": "..."
            }
        }
    }
}
```

### Cryptography

- **Certificates** - Certificate management, validation, and operations

Resolving the configured certificate:
```csharp
var resolver = provider.GetRequiredService<IKeyResolver>();
resolver.LoadCertificate("MyCertificate");
```

### Data Access

- **Unit of Work** - Repository and unit of work patterns for data access
- **PostgreSQL Support** - PostgreSQL-specific implementations and extensions
- **Database Migrations** - Database schema versioning and migrations

Registering unit of work:
```csharp
builder.Services
    .AddUnitOfWork(opt =>
    {
        opt.BindDbContext<MyContext>();
    });
```

Adding PostgreSQL:
```csharp
builder.Services
    .AddConnectionResolver()
    .WithPostgreSql();
```

Adding migrations:
```csharp
builder.Services
    .AddMigrations(typeof(Program).Assembly);
```

### Email

- **SMTP Integration** - SMTP-based email delivery service

Adding SMTP:
```csharp
builder.Services
    .AddConnectionResolver()
    .WithSmtp();
```

### Messaging

- **Core Messaging** - Base messaging abstractions and utilities
- **Message Inbox** - Incoming message handling and processing
- **Message Outbox** - Outgoing message queue and delivery
- **RabbitMQ Support** - RabbitMQ broker integration

Registering messaging:
```csharp
builder.Services
    .AddMessaging(opt =>
    {
        opt.Publishers.AddQueue("q", "physical.name");
        opt.Listeners.AddQueue("q", "physical.name");
    })
    .UseConnection("MyMessageBrokerConnectionId")
    .AddConsumersFromAssemblies(typeof(Program).Assembly);
```

### Notifications

- **Core Notifications** - Notification framework and abstractions
- **Email Notifications** - Email-based notification delivery
- **Webhook Notifications** - HTTP webhook-based notifications

Registering notifications:
```csharp
builder.Services
    .AddNotifications("MyDatabaseConnectionId", "MyMessageBrokerConnectionId", opt =>
    {
        opt.AddNotification<TestDomainEvent, TestDomainEventRouter>("test.domain.event", type =>
        {
            type.HasDisplayName("Test Domain Event");
            type.Supports<TestDomainEvent, WebhookChannel, TestDomainEventWebhookFormatter>();
            type.SupportsDigest<TestDomainEvent, WebhookChannel, TestDomainEventDigestWebhookFormatter>();
        });

        opt.AddEmailChannel(); //Requires Notifications.Email
        opt.AddWebhookChannel(); //Requires Notifications.Webhook
    });
```

## License

This application is licensed under the [MIT License](LICENSE).
