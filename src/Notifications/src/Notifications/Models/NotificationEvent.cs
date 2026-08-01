using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class NotificationEvent : IEntity
{
    public virtual string Id { get; set; } = null!;

    public virtual string Name { get; set; } = null!;

    public virtual bool IsAvailable { get; set; }

    public virtual DateTimeOffset LastSeenAt { get; set; }

    public virtual IDictionary<string, object?> ConfigSchema { get; set; } = new Dictionary<string, object?>();

    public virtual List<NotificationChannelSchedule> Supports { get; set; } = [];
}

public class NotificationEventConfiguration : IEntityTypeConfiguration<NotificationEvent>
{
    public void Configure(
        EntityTypeBuilder<NotificationEvent> entity)
    {
        entity.ToTable("events", schema: "notification");

        entity.Property(e => e.Id)
            .HasColumnName("event_id");

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
