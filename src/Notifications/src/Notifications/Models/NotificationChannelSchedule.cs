using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class NotificationChannelSchedule : IEntity
{
    public virtual string EventId { get; set; } = null!;

    public virtual NotificationEvent? Event { get; set; }

    public virtual string ChannelId { get; set; } = null!;

    public virtual NotificationChannel? Channel { get; set; }

    public virtual string ScheduleId { get; set; } = null!;

    public virtual NotificationSchedule? Schedule { get; set; }
}

public class NotificationChannelScheduleConfiguration : IEntityTypeConfiguration<NotificationChannelSchedule>
{
    public void Configure(
        EntityTypeBuilder<NotificationChannelSchedule> entity)
    {
        entity.ToTable("event_supports", schema: "notification");

        entity.HasKey(e => new { e.EventId, e.ChannelId, e.ScheduleId });

        entity.Property(e => e.EventId)
            .HasColumnName("event_id");

        entity.HasOne(e => e.Event)
            .WithMany(e => e.Supports)
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
    }
}
