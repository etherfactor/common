using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

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
