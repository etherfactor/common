using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class NotificationSubscription : IEntity
{
    public virtual long Id { get; set; }

    public virtual string UserId { get; set; } = null!;

    public virtual string EventType { get; set; } = null!;

    public virtual string ChannelKey { get; set; } = null!;

    public virtual string ChannelConfigRaw { get; set; } = null!;

    public virtual string ScheduleType { get; set; } = null!;

    public virtual string ScheduleConfigRaw { get; set; } = null!;

    public virtual bool IsEnabled { get; set; }

    public virtual DateTimeOffset? LastNotificationAt { get; set; }

    public virtual DateTimeOffset? NextNotificationAt { get; set; }
}

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

        entity.Property(e => e.ChannelConfigRaw)
            .HasColumnName("channel_config");

        entity.Property(e => e.ScheduleType)
            .HasColumnName("schedule_type");

        entity.Property(e => e.ScheduleConfigRaw)
            .HasColumnName("schedule_config");

        entity.Property(e => e.IsEnabled)
            .HasColumnName("is_enabled");

        entity.Property(e => e.LastNotificationAt)
            .HasColumnName("last_notification_at");

        entity.Property(e => e.NextNotificationAt)
            .HasColumnName("next_notification_at");
    }
}
