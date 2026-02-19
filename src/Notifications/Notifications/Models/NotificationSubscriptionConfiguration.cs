using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class NotificationSubscriptionConfiguration : IEntityTypeConfiguration<NotificationSubscription>
{
    public void Configure(
        EntityTypeBuilder<NotificationSubscription> entity)
    {
        entity.ToTable("notification_subscriptions");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasColumnName("notification_subscription_id");

        entity.Property(e => e.UserId)
            .HasColumnName("user_id");

        entity.Property(e => e.EventType)
            .HasColumnName("event_type");

        entity.Property(e => e.ChannelKey)
            .HasColumnName("channel_key");

        entity.Property(e => e.ScheduleType)
            .HasColumnName("schedule_type");

        entity.Property(e => e.ScheduleConfig)
            .HasColumnName("schedule_config");

        entity.Property(e => e.IsEnabled)
            .HasColumnName("is_enabled");
    }
}
