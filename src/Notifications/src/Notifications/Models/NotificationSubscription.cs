using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class NotificationSubscription : IEntity
{
    public virtual long Id { get; set; }

    public virtual string UserId { get; set; } = null!;

    public virtual string EventId { get; set; } = null!;

    public virtual NotificationEvent Event { get; set; } = null!;

    public virtual IDictionary<string, object?> EventConfig { get; set; } = new Dictionary<string, object?>();

    public virtual string ChannelId { get; set; } = null!;

    public virtual NotificationChannel Channel { get; set; } = null!;

    public virtual IDictionary<string, object?> ChannelConfig { get; set; } = new Dictionary<string, object?>();

    public virtual string ScheduleId { get; set; } = null!;

    public virtual NotificationSchedule Schedule { get; set; } = null!;

    public virtual IDictionary<string, object?> ScheduleConfig { get; set; } = new Dictionary<string, object?>();

    public virtual bool IsEnabled { get; set; }

    public virtual DateTimeOffset? LastNotificationAt { get; set; }

    public virtual DateTimeOffset? NextNotificationAt { get; set; }
}

public class NotificationSubscriptionConfiguration : IEntityTypeConfiguration<NotificationSubscription>
{
    public void Configure(
        EntityTypeBuilder<NotificationSubscription> entity)
    {
        entity.ToTable("subscriptions", schema: "notification");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasColumnName("subscription_id");

        entity.Property(e => e.UserId)
            .HasColumnName("user_id");

        entity.Property(e => e.EventId)
            .HasColumnName("event_id");

        entity.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId);

        entity.Property(e => e.EventConfig)
            .HasColumnName("event_config");

        entity.Property(e => e.ChannelId)
            .HasColumnName("channel_id");

        entity.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.ChannelId);

        entity.Property(e => e.ChannelConfig)
            .HasColumnName("channel_config");

        entity.Property(e => e.ScheduleId)
            .HasColumnName("schedule_id");

        entity.HasOne(e => e.Schedule)
            .WithMany()
            .HasForeignKey(e => e.ScheduleId);

        entity.Property(e => e.ScheduleConfig)
            .HasColumnName("schedule_config");

        entity.Property(e => e.IsEnabled)
            .HasColumnName("is_enabled");

        entity.Property(e => e.LastNotificationAt)
            .HasColumnName("last_notification_at");

        entity.Property(e => e.NextNotificationAt)
            .HasColumnName("next_notification_at");
    }
}
