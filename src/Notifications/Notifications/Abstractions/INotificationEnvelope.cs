namespace EtherGizmos.Common.Abstractions;

public interface INotificationEnvelope<TMethod>
    where TMethod : DeliveryMethod;
