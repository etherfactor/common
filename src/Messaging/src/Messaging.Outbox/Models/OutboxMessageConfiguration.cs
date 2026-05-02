using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public virtual void Configure(
        EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("outbox_id");

        builder.Property(e => e.MessageId)
            .HasColumnName("message_id");

        builder.Property(e => e.QueuedAt)
            .HasColumnName("queued_at_utc");

        builder.Property(e => e.AvailableAt)
            .HasColumnName("available_at_utc");

        builder.Property(e => e.PublishedAt)
            .HasColumnName("published_at_utc");

        builder.Property(e => e.Status)
            .HasColumnName("outbox_status_type_id");

        builder.Property(e => e.LogicalDestinationName)
            .HasColumnName("logical_destination_name");

        builder.Property(e => e.Type)
            .HasColumnName("type");

        builder.Property(e => e.Payload)
            .HasColumnName("payload");

        builder.Property(e => e.Headers)
            .HasColumnName("headers");

        builder.Property(e => e.AttemptCount)
            .HasColumnName("attempt_count");

        builder.Property(e => e.LastAttemptAt)
            .HasColumnName("last_attempt_at_utc");

        builder.Property(e => e.LastError)
            .HasColumnName("last_error");

        builder.Property(e => e.LockId)
            .HasColumnName("lock_id");

        builder.Property(e => e.LockedBy)
            .HasColumnName("locked_by");

        builder.Property(e => e.LockedUntil)
            .HasColumnName("locked_until_utc");
    }
}
