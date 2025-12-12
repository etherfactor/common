namespace EtherGizmos.Common.Abstractions;

public interface IMessageMiddleware
{
    Task InvokeAsync(ReceivedMessage message, Func<Task> next);
}
