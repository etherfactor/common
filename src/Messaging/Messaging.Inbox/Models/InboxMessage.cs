namespace EtherGizmos.Common.Models;

public class InboxMessage
{
    public virtual int Id { get; set; }

    public virtual string MessageId { get; set; } = null!;

    public virtual DateTimeOffset ReceivedAt { get; set; }

    public virtual DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>
    /// For a queue: 'logical-name'. For a topic: 'logical-name/subscription'.
    /// </summary>
    public virtual string Subscription { get; set; } = null!;

    public virtual string ConsumerType { get; set; } = null!;

    public virtual InboxStatusType Status { get; set; }

    public virtual string LogicalSourceName { get; set; } = null!;

    public virtual string Type { get; set; } = null!;

    public virtual string Payload { get; set; } = null!;

    public virtual IDictionary<string, object?> Headers { get; set; } = new Dictionary<string, object?>();

    public virtual int AttemptCount { get; set; }

    public virtual DateTimeOffset? LastAttemptAt { get; set; }

    public virtual string? LastError { get; set; }

    public virtual Guid? LockId { get; set; }

    public virtual string? LockedBy { get; set; }

    public virtual DateTimeOffset? LockedUntil { get; set; }
}
