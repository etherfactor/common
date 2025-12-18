namespace EtherGizmos.Common.Models;

public class OutboxMessage
{
    public virtual int Id { get; set; }

    public virtual string MessageId { get; set; } = null!;

    public virtual DateTimeOffset QueuedAt { get; set; }

    public virtual DateTimeOffset AvailableAt { get; set; }

    public virtual DateTimeOffset? PublishedAt { get; set; }

    public virtual OutboxStatusType Status { get; set; }

    public virtual string LogicalDestinationName { get; set; } = null!;

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
