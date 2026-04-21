using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class Notification : IEntity
{
    public long Id { get; set; }

    public Guid EventId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SentAt { get; set; }

    public long NotificationSubscriptionId { get; set; }

    public NotificationSubscription NotificationSubscription { get; set; } = null!;

    public bool IsDerived { get; set; }

    public string PayloadType { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public NotificationStatusType StatusType { get; set; }

    public int AttemptCount { get; set; }
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

        entity.Property(e => e.StatusType)
            .HasColumnName("notification_status_type_id");

        entity.Property(e => e.AttemptCount)
            .HasColumnName("attempt_count");
    }
}
