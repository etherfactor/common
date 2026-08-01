using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class NotificationChannel : IEntity
{
    public virtual string Id { get; set; } = null!;

    public virtual string Name { get; set; } = null!;

    public virtual bool IsAvailable { get; set; }

    public virtual DateTimeOffset LastSeenAt { get; set; }

    public virtual IDictionary<string, object?> ConfigSchema { get; set; } = new Dictionary<string, object?>();
}

public class NotificationChannelConfiguration : IEntityTypeConfiguration<NotificationChannel>
{
    public void Configure(
        EntityTypeBuilder<NotificationChannel> entity)
    {
        entity.ToTable("channels", schema: "notification");

        entity.Property(e => e.Id)
            .HasColumnName("channel_id");

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
