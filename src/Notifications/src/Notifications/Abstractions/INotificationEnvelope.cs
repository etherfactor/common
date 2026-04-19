namespace EtherGizmos.Common.Abstractions;

public interface INotificationEnvelope;

public interface INotificationEnvelope<TMethod>
    : INotificationEnvelope
    where TMethod : DeliveryMethod;
