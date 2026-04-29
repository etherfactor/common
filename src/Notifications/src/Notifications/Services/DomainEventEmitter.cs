using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;
using System.Diagnostics;

namespace EtherGizmos.Common.Services;

internal class DomainEventEmitter : IDomainEventEmitter
{
    private readonly IOptionsMonitor<NotificationTypeOptions> _typeOptions;
    private readonly IDomainEventSerializer _eventSerializer;
    private readonly IMessageSender _messageSender;

    public DomainEventEmitter(
        IOptionsMonitor<NotificationTypeOptions> typeOptions,
        IDomainEventSerializer eventSerializer,
        IMessageSender messageSender)
    {
        _typeOptions = typeOptions;
        _eventSerializer = eventSerializer;
        _messageSender = messageSender;
    }

    public async Task EmitAsync(
        IDomainEvent @event,
        IEnumerable<AudienceKey> audiences,
        DomainEventEmissionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        using var activity = ActivitySources.Notifications.StartActivity(
            $"Emit domain event of type {@event.GetType().FullName}",
            ActivityKind.Producer);

        var lookupType = @event.GetType();
        if (lookupType.IsGenericType
            && lookupType.GetGenericTypeDefinition() == typeof(Digest<>))
        {
            lookupType = lookupType.GetGenericArguments()[0];
        }

        var eventType = _typeOptions.CurrentValue.EventTypeMap
            .FirstOrDefault(e => e.Value == lookupType)
            .Key
            ?? throw new InvalidOperationException($"Unknown event type '{@event.GetType()}'");

        var serialized = _eventSerializer.Serialize(@event);
        var message = new DomainEventMessage()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            EventType = eventType,
            Audiences = [.. audiences],
            IsDerived = options?.IsDerived ?? false,
            PayloadType = serialized.Type,
            Payload = serialized.Payload,
        };

        var context = ActivityContextPropagator.Pack(Activity.Current);
        await _messageSender.SendAsync(
            NotificationConstants.DomainEventsLogicalName,
            message,
            options: new MessageSendOptions()
            {
                Headers = context.ToImmutableDictionary(),
            },
            cancellationToken: cancellationToken);
    }
}
