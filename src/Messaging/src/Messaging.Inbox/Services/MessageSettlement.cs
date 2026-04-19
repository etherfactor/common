using EtherGizmos.Common.Abstractions;
using System.Collections.Concurrent;

namespace EtherGizmos.Common.Services;

internal class MessageSettlement : IMessageSettlement
{
    private const int IsCompletedValue = 1;
    private const int IsAbandonedValue = 2;
    private const int IsDeadLetteredValue = 3;
    private readonly TimeSpan MissingRegistrationGrace = TimeSpan.FromSeconds(1);

    private IMessageActions _actions = null!;

    private int _expected = 0;
    private long _lastRegistrationTicks = 0;
    private readonly ConcurrentDictionary<string, int> _registered = [];
    private readonly ConcurrentDictionary<string, int> _results = [];

    private int _waiter = 0;
    private int _settled = 0;

    public bool IsAbandoned => _results.Values.Max() == IsAbandonedValue;

    public bool IsCompleted => _results.Values.Max() == IsCompletedValue;

    public bool IsDeadLettered => _results.Values.Max() == IsDeadLetteredValue;

    public void Abandon(string consumerName)
        => SetResultOnce(consumerName, IsAbandonedValue);

    public void Complete(string consumerName)
        => SetResultOnce(consumerName, IsCompletedValue);

    public void DeadLetter(string consumerName)
        => SetResultOnce(consumerName, IsDeadLetteredValue);

    private void SetResultOnce(
        string consumerName,
        int result)
        => _results.AddOrUpdate(
            consumerName,
            result,
            (_, _) => throw new InvalidOperationException($"Already acted on {consumerName}"));

    public async Task FinalizeAsync(CancellationToken cancellationToken = default)
    {
        // Don’t finalize until we have at least one registration
        if (_registered.IsEmpty)
            return;

        // Don’t finalize until all registered consumers have produced a decision
        var r = _registered.Count;
        if (_results.Count != r)
            return;

        // Happy path: if we know expected and have them all, settle immediately.
        var e = Volatile.Read(ref _expected);
        if (e > 0 && r == e)
        {
            await TrySettleOnceAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        // If expected is unknown (0), choose a policy:
        // - either "settle immediately when all registered decided"
        // - or treat as misconfig and abandon
        // I’d settle immediately if expected==0.
        if (e == 0)
        {
            await TrySettleOnceAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        // Here: r < e and all currently registered decided.
        // Wait briefly to see if more registrations arrive; only ONE waiter does this.
        if (Interlocked.Exchange(ref _waiter, 1) != 0)
            return;

        try
        {
            while (true)
            {
                var last = Interlocked.Read(ref _lastRegistrationTicks);
                if (last == 0) last = DateTimeOffset.UtcNow.UtcTicks;

                var now = DateTimeOffset.UtcNow;

                var elapsed = now - new DateTimeOffset(last, TimeSpan.Zero);
                if (elapsed >= MissingRegistrationGrace)
                    break;

                // Sleep until grace elapses or cancellation
                var remaining = MissingRegistrationGrace - elapsed;
                if (remaining < TimeSpan.FromMilliseconds(10))
                    remaining = TimeSpan.FromMilliseconds(10);

                await Task.Delay(remaining, cancellationToken).ConfigureAwait(false);

                // If more registrations came in, exit and let later finalizers handle it.
                var newR = _registered.Count;
                if (newR != r)
                    return;

                // If a new registration came in but hasn't decided yet, also exit.
                if (_results.Count != r)
                    return;

                // else loop until grace is satisfied
            }

            // Grace elapsed with no new registrations: abandon (or deadletter) to retry.
            // Only settle once.
            await TrySettleOnceAsync(cancellationToken, forceAbandon: true).ConfigureAwait(false);
        }
        finally
        {
            // allow a waiter again if we returned early due to new registrations
            Interlocked.Exchange(ref _waiter, 0);
        }
    }

    private async Task TrySettleOnceAsync(CancellationToken ct, bool forceAbandon = false)
    {
        if (Interlocked.Exchange(ref _settled, 1) != 0)
            return;

        var actions = _actions ?? throw new InvalidOperationException("Settlement actions not captured.");

        int max = 0;
        foreach (var v in _results.Values)
            max = Math.Max(max, v);

        if (forceAbandon)
            max = Math.Max(max, IsAbandonedValue);

        if (max == IsDeadLetteredValue) await actions.DeadLetterAsync(ct).ConfigureAwait(false);
        else if (max == IsAbandonedValue) await actions.AbandonAsync(ct).ConfigureAwait(false);
        else await actions.CompleteAsync(ct).ConfigureAwait(false);
    }

    private object _expectedLock = new();
    public void SetExpected(
        Func<int> expected)
    {
        if (_expected != 0)
            return;

        lock (_expectedLock)
        {
            if (_expected == 0)
                _expected = expected();
        }
    }

    public void Register(
        string consumerName,
        IMessageActions actions)
    {
        _registered.AddOrUpdate(consumerName, 0, (_, _) => 0);
        Interlocked.CompareExchange(ref _actions, actions, null);
        Interlocked.CompareExchange(ref _lastRegistrationTicks, DateTimeOffset.UtcNow.UtcTicks, Volatile.Read(ref _lastRegistrationTicks));
    }
}
