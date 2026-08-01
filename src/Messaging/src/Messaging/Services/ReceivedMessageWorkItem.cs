using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal sealed class ReceivedMessageWorkItem
{
    private readonly TaskCompletionSource _completion = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public ReceivedMessage Message { get; }
    public Task Completion => _completion.Task;

    public ReceivedMessageWorkItem(ReceivedMessage message)
    {
        Message = message;
    }

    public void Complete()
        => _completion.TrySetResult();
}

internal readonly record struct MessageDispatchHandle(
    bool Accepted,
    Task Completion);
