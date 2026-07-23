using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public record NotificationChannelSchedule
{
    public required string EventId { get; init; }

    public NotificationEvent? Event { get; init; }

    public required string ChannelId { get; init; }

    public NotificationChannel? Channel { get; init; }

    public required string ScheduleId { get; init; }

    public NotificationSchedule? Schedule { get; init; }
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
