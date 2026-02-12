namespace EtherGizmos.Common.Abstractions;

internal interface IDomainEventSerializer
{
    object Deserialize(string type, string payload);
    (string Type, string Payload) Serialize(object data);
}