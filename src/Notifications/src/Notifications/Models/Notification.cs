using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class Notification : IEntity
{
    public virtual long Id { get; set; }

    public virtual Guid OccurrenceId { get; set; }

    public virtual DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual DateTimeOffset? SentAt { get; set; }

    public virtual long SubscriptionId { get; set; }

    public virtual NotificationSubscription Subscription { get; set; } = null!;

    public virtual string EventId { get; set; } = null!;

    public virtual NotificationEvent Event { get; set; } = null!;

    public virtual string ChannelId { get; set; } = null!;

    public virtual NotificationChannel Channel { get; set; } = null!;

    public virtual string ScheduleId { get; set; } = null!;

    public virtual NotificationSchedule Schedule { get; set; } = null!;

    public virtual bool IsDerived { get; set; }

    public virtual string PayloadType { get; set; } = null!;

    public virtual string Payload { get; set; } = null!;

    public virtual NotificationStatusType Status { get; set; }

    public virtual IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

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
        entity.ToTable("instances", schema: "notification");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasColumnName("instance_id");

        entity.Property(e => e.OccurrenceId)
            .HasColumnName("occurrence_id");

        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at_utc");

        entity.Property(e => e.SentAt)
            .HasColumnName("sent_at_utc");

        entity.Property(e => e.SubscriptionId)
            .HasColumnName("subscription_id");

        entity.HasOne(e => e.Subscription)
            .WithMany()
            .HasForeignKey(e => e.SubscriptionId);

        entity.Property(e => e.EventId)
            .HasColumnName("event_id");

        entity.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId);

        entity.Property(e => e.ChannelId)
            .HasColumnName("channel_id");

        entity.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.ChannelId);

        entity.Property(e => e.ScheduleId)
            .HasColumnName("schedule_id");

        entity.HasOne(e => e.Schedule)
            .WithMany()
            .HasForeignKey(e => e.ScheduleId);

        entity.Property(e => e.IsDerived)
            .HasColumnName("is_derived");

        entity.Property(e => e.PayloadType)
            .HasColumnName("payload_type");

        entity.Property(e => e.Payload)
            .HasColumnName("payload");

        entity.Property(e => e.Status)
            .HasColumnName("status_type_id");

        entity.Property(e => e.Headers)
            .HasColumnName("headers");

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
