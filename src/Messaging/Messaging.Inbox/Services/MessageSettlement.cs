using EtherGizmos.Common.Abstractions;
using System.Collections.Concurrent;

namespace EtherGizmos.Common.Services;

internal class MessageSettlement : IMessageSettlement
{
    private const int IsCompletedValue = 1;
    private const int IsAbandonedValue = 2;
    private const int IsDeadLetteredValue = 3;

    private IMessageActions _actions = null!;

    private long _closesAtTicks = 0;
    private int _registered = 0;
    private ConcurrentDictionary<string, int> _results = [];

    private int _waiter = 0;
    private int _settled = 0;

    public bool IsAbandoned => _results.Values.Max() == IsAbandonedValue;

    public bool IsCompleted => _results.Values.Max() == IsCompletedValue;

    public bool IsDeadLettered => _results.Values.Max() == IsDeadLetteredValue;

    public void Abandon(string consumerName)
        => _results.AddOrUpdate(
            consumerName,
            IsAbandonedValue,
            (_, _) => throw new InvalidOperationException($"Already acted on {consumerName}"));

    public void Complete(string consumerName)
        => _results.AddOrUpdate(
            consumerName,
            IsCompletedValue,
            (_, _) => throw new InvalidOperationException($"Already acted on {consumerName}"));

    public void DeadLetter(string consumerName)
        => _results.AddOrUpdate(
            consumerName,
            IsDeadLetteredValue,
            (_, _) => throw new InvalidOperationException($"Already acted on {consumerName}"));

    public async Task FinalizeAsync(
        CancellationToken cancellationToken = default)
    {
        var closesAt = Interlocked.Read(ref _closesAtTicks);
        if (closesAt == 0)
            return; // nothing registered; shouldn't happen normally

        var nowTicks = DateTimeOffset.UtcNow.UtcTicks;

        // If we are before the close time, elect a single waiter to delay.
        if (nowTicks < closesAt)
        {
            if (Interlocked.Exchange(ref _waiter, 1) != 0)
                return; // someone else is responsible for waiting/settling

            var delay = TimeSpan.FromTicks(closesAt - nowTicks);
            // Use Delay with cancellation if you want fast shutdown; either is fine.
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        // Settle once
        if (Interlocked.Exchange(ref _settled, 1) != 0)
            return;

        var actions = _actions ?? throw new InvalidOperationException("Settlement actions not captured.");

        if (IsDeadLettered) await actions.DeadLetterAsync(cancellationToken).ConfigureAwait(false);
        else if (IsAbandoned) await actions.AbandonAsync(cancellationToken).ConfigureAwait(false);
        else if (IsCompleted) await actions.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Register(string consumerName, IMessageActions actions)
    {
        Interlocked.Increment(ref _registered);
        Interlocked.CompareExchange(ref _actions, actions, null);
    }
}
