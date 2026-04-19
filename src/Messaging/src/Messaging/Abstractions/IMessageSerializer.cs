namespace EtherGizmos.Common.Abstractions;

public interface IMessageSerializer
{
    string Serialize<TMessage>(TMessage message)
        where TMessage : class, new();

    TMessage Deserialize<TMessage>(string message)
        where TMessage : class, new();
}
