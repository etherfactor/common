using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class Notification : IEntity
{
    public virtual long Id { get; set; }

    public virtual Guid EventId { get; set; }

    public virtual DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual DateTimeOffset? SentAt { get; set; }

    public virtual long NotificationSubscriptionId { get; set; }

    public virtual NotificationSubscription NotificationSubscription { get; set; } = null!;

    public virtual bool IsDerived { get; set; }

    public virtual string PayloadType { get; set; } = null!;

    public virtual string Payload { get; set; } = null!;

    public virtual NotificationStatusType Status { get; set; }

    public virtual int AttemptCount { get; set; }

    public virtual DateTimeOffset? LastAttemptAt { get; set; }

    public virtual string? LastError { get; set; }

    public virtual Guid? LockId { get; set; }

    public virtual string? LockedBy { get; set; }

    public virtual DateTimeOffset? LockedUntil { get; set; }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(
        EntityTypeBuilder<Notification> entity)
    {
        entity.ToTable("notifications");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.EventId)
            .HasColumnName("event_id");

        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        entity.Property(e => e.SentAt)
            .HasColumnName("sent_at");

        entity.Property(e => e.Id)
            .HasColumnName("notification_id");

        entity.Property(e => e.NotificationSubscriptionId)
            .HasColumnName("notification_subscription_id");

        entity.Property(e => e.IsDerived)
            .HasColumnName("is_derived");

        entity.Property(e => e.PayloadType)
            .HasColumnName("payload_type");

        entity.Property(e => e.Payload)
            .HasColumnName("payload");

        entity.Property(e => e.Status)
            .HasColumnName("notification_status_type_id");

        entity.Property(e => e.AttemptCount)
            .HasColumnName("attempt_count");

        entity.Property(e => e.LastAttemptAt)
            .HasColumnName("last_attempt_at_utc");

        entity.Property(e => e.LastError)
            .HasColumnName("last_error");

        entity.Property(e => e.LockId)
            .HasColumnName("lock_id");

        entity.Property(e => e.LockedBy)
            .HasColumnName("locked_by");

        entity.Property(e => e.LockedUntil)
            .HasColumnName("locked_until_utc");
    }
}
