using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;

namespace EtherGizmos.Common.Services;

internal class OutboxMessagePublisher : IOutboxMessagePublisher
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IMessageSender _sender;

    public OutboxMessagePublisher(
        IUnitOfWorkFactory uowFactory,
        ITransportMessageSender sender)
    {
        _uowFactory = uowFactory;
        _sender = sender;
    }

    public async Task<bool> PublishAsync(
        CancellationToken cancellationToken = default)
    {
        using var uow = _uowFactory.Create();
        var messageRepo = uow.Repository<OutboxMessage>();

        var lockId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var lockedUntil = now.Add(TimeSpan.FromSeconds(30));

        var isClaimed = false;
        var claimedCount = 0;
        do
        {
            var candidateIds = await messageRepo.Data
                .Where(e =>
                    e.Status == OutboxStatusType.Pending &&
                    e.AvailableAt <= DateTimeOffset.UtcNow &&
                    (e.LockedUntil == null || e.LockedUntil < DateTimeOffset.UtcNow) &&
                    e.Payload != null)
                .OrderBy(e => e.AvailableAt)
                .ThenBy(e => e.Id)
                .Select(e => e.Id)
                .Take(100)
                .ToListAsync(cancellationToken: cancellationToken);

            await messageRepo.Data
                .Where(e =>
                    candidateIds.Contains(e.Id) &&
                    e.Status == OutboxStatusType.Pending &&
                    e.AvailableAt <= DateTimeOffset.UtcNow &&
                    (e.LockedUntil == null || e.LockedUntil < DateTimeOffset.UtcNow))
                .ExecuteUpdateAsync(e => e
                    .SetProperty(e => e.LockId, _ => lockId)
                    .SetProperty(e => e.LockedBy, _ => Environment.MachineName)
                    .SetProperty(e => e.LockedUntil, _ => lockedUntil),
                    cancellationToken: cancellationToken);

            var claimed = await messageRepo.Data
                .AsNoTracking()
                .Where(e => e.LockId == lockId)
                .ToListAsync(cancellationToken: cancellationToken);

            claimedCount = claimed.Count;
            if (claimedCount > 0) isClaimed = true;

            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 8,
                CancellationToken = cancellationToken,
            };

            await Parallel.ForEachAsync(claimed, parallelOptions, async (message, ct) =>
            {
                var attempt = DateTimeOffset.UtcNow;

                using var uow = _uowFactory.Create();
                var messageRepo = uow.Repository<OutboxMessage>();

                try
                {
                    await messageRepo.Data
                        .Where(e =>
                            e.Id == message.Id &&
                            e.LockId == lockId)
                        .ExecuteUpdateAsync(e => e
                            .SetProperty(e => e.Status, _ => OutboxStatusType.InFlight),
                            cancellationToken: ct);

                    var toSend = new SentMessage()
                    {
                        MessageId = message.MessageId,
                        Type = message.Type,
                        Body = message.Payload,
                        Headers = message.Headers.ToImmutableDictionary(),
                        LogicalDestinationName = message.LogicalDestinationName,
                    };

                    await _sender.SendAsync(toSend, ct);

                    await messageRepo.Data
                        .Where(e =>
                            e.Id == message.Id &&
                            e.LockId == lockId)
                        .ExecuteUpdateAsync(e => e
                            .SetProperty(e => e.PublishedAt, _ => attempt)
                            .SetProperty(e => e.Status, _ => OutboxStatusType.Published)
                            .SetProperty(e => e.AttemptCount, e => e.AttemptCount + 1)
                            .SetProperty(e => e.LastAttemptAt, _ => attempt)
                            .SetProperty(e => e.LastError, _ => null)
                            .SetProperty(e => e.LockId, _ => null)
                            .SetProperty(e => e.LockedBy, _ => null)
                            .SetProperty(e => e.LockedUntil, _ => null),
                            cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    var (backoff, isDead) = ComputeBackoff(message.AttemptCount + 1);
                    var available = attempt.Add(backoff);
                    await messageRepo.Data
                        .Where(e =>
                            e.Id == message.Id &&
                            e.LockId == lockId)
                        .ExecuteUpdateAsync(e => e
                            .SetProperty(e => e.AvailableAt, _ => available)
                            .SetProperty(e => e.PublishedAt, _ => attempt)
                            .SetProperty(e => e.Status, _ => isDead ? OutboxStatusType.Failed : OutboxStatusType.Pending)
                            .SetProperty(e => e.AttemptCount, e => e.AttemptCount + 1)
                            .SetProperty(e => e.LastAttemptAt, _ => attempt)
                            .SetProperty(e => e.LastError, _ => ex.ToString())
                            .SetProperty(e => e.LockId, _ => null)
                            .SetProperty(e => e.LockedBy, _ => null)
                            .SetProperty(e => e.LockedUntil, _ => null),
                            cancellationToken: ct);
                }
            });
        }
        while (claimedCount > 0);

        return isClaimed;
    }

    private static (TimeSpan Delay, bool IsDead) ComputeBackoff(
        int attemptCount)
    {
        // attemptCount is AFTER increment
        // 1 -> 2s, 2 -> 5s, 3 -> 15s, 4 -> 30s, 5 -> 60s, then cap
        return attemptCount switch
        {
            <= 1 => (TimeSpan.FromSeconds(2), false),
            2 => (TimeSpan.FromSeconds(5), false),
            3 => (TimeSpan.FromSeconds(15), false),
            4 => (TimeSpan.FromSeconds(30), false),
            5 => (TimeSpan.FromMinutes(1), false),
            _ => (TimeSpan.FromMinutes(5), true),
        };
    }
}
