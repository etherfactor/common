using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EtherGizmos.Common.Services;

internal class InboxDeduplicateMiddleware : IMessageMiddleware
{
    private readonly IUnitOfWorkFactory _uowFactory;

    public InboxDeduplicateMiddleware(
        IUnitOfWorkFactory uowFactory)
    {
        _uowFactory = uowFactory;
    }

    public async Task InvokeAsync(
        ReceivedMessage message,
        Func<Task> next)
    {
        await EnsureCreatedAsync(message);

        var attempted = DateTimeOffset.UtcNow;

        var lockId = Guid.NewGuid();
        var lockedUntil = attempted.AddSeconds(30);

        using var uow = _uowFactory.Create();
        var messageRepo = uow.Repository<InboxMessage>();

        var claimed = await messageRepo.Data
            .Where(e =>
                e.MessageId == message.MessageId &&
                e.Subscription == message.SubscriptionName &&
                e.ConsumerName == message.ConsumerName &&
                e.Status != InboxStatusType.Processed)
            .ExecuteUpdateAsync(e => e
                .SetProperty(e => e.Status, _ => InboxStatusType.InFlight)
                .SetProperty(e => e.AttemptCount, e => e.AttemptCount + 1)
                .SetProperty(e => e.LastAttemptAt, _ => attempted)
                .SetProperty(e => e.LockId, _ => lockId)
                .SetProperty(e => e.LockedBy, _ => Environment.MachineName)
                .SetProperty(e => e.LockedUntil, _ => lockedUntil));

        //We didn't claim the message, so it may have already been processed
        if (claimed == 0)
            return;

        try
        {
            await next();

            if (!message.Actions.Invoked)
                await message.Actions.CompleteAsync();

            var processedAt = DateTimeOffset.UtcNow;

            await messageRepo.Data
                .Where(e =>
                    e.MessageId == message.MessageId &&
                    e.Subscription == message.SubscriptionName &&
                    e.ConsumerName == message.ConsumerName &&
                    e.LockId == lockId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.Status, _ => message.Actions.Decision == MessageDecision.Complete
                        ? InboxStatusType.Processed
                        : InboxStatusType.Pending)
                    .SetProperty(e => e.ProcessedAt, _ => processedAt)
                    .SetProperty(e => e.LastError, _ => null)
                    .SetProperty(e => e.LockId, _ => null)
                    .SetProperty(e => e.LockedBy, _ => null)
                    .SetProperty(e => e.LockedUntil, _ => null));
        }
        catch (Exception ex)
        {
            if (!message.Actions.Invoked)
                await message.Actions.AbandonAsync();

            await messageRepo.Data
                .Where(e =>
                    e.MessageId == message.MessageId &&
                    e.Subscription == message.SubscriptionName &&
                    e.ConsumerName == message.ConsumerName &&
                    e.LockId == lockId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.Status, _ => InboxStatusType.Pending)
                    .SetProperty(e => e.LastError, _ => ex.ToString())
                    .SetProperty(e => e.LockId, _ => null)
                    .SetProperty(e => e.LockedBy, _ => null)
                    .SetProperty(e => e.LockedUntil, _ => null));

            throw;
        }
    }

    private async Task EnsureCreatedAsync(
        ReceivedMessage message)
    {
        using var uow = _uowFactory.Create();
        var messageRepo = uow.Repository<InboxMessage>();

        try
        {
            var exists = await messageRepo.Data
                .AnyAsync(e =>
                    e.MessageId == message.MessageId &&
                    e.Subscription == message.SubscriptionName &&
                    e.ConsumerName == message.ConsumerName);

            if (exists)
                return;

            var inbox = new InboxMessage
            {
                MessageId = message.MessageId,
                ReceivedAt = DateTimeOffset.UtcNow,
                Subscription = message.SubscriptionName,
                ConsumerName = message.ConsumerName!, // should be required
                Status = InboxStatusType.Pending,
                LogicalSourceName = message.LogicalSourceName,
                Type = message.Type,
                Payload = message.Body,
                Headers = message.Headers.ToDictionary(),
            };

            messageRepo.Add(inbox);
            await uow.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            //Someone else inserted it first
        }
    }
}
