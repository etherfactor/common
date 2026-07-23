using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public record NotificationSchedule : IEntity
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required bool IsAvailable { get; init; }

    public required DateTimeOffset LastSeenAt { get; init; }

    public required IDictionary<string, object?> ConfigSchema { get; init; }
}

public class NotificationScheduleConfiguration : IEntityTypeConfiguration<NotificationSchedule>
{
    public void Configure(
        EntityTypeBuilder<NotificationSchedule> entity)
    {
        entity.ToTable("schedules", schema: "notification");

        entity.Property(e => e.Id)
            .HasColumnName("schedule_id");

        entity.Property(e => e.Name)
            .HasColumnName("name");

        entity.Property(e => e.IsAvailable)
            .HasColumnName("is_available");

        entity.Property(e => e.LastSeenAt)
            .HasColumnName("last_seen_at_utc");

        entity.Property(e => e.ConfigSchema)
            .HasColumnName("config_schema");
    }
}
