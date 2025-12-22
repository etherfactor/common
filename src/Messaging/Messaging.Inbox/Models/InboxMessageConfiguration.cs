using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtherGizmos.Common.Models;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public virtual void Configure(
        EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("inbox_id");

        builder.Property(e => e.MessageId)
            .HasColumnName("message_id");

        builder.Property(e => e.ReceivedAt)
            .HasColumnName("received_at_utc");

        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at_utc");

        builder.Property(e => e.Subscription)
            .HasColumnName("subscription");

        builder.Property(e => e.ConsumerName)
            .HasColumnName("consumer_name");

        builder.Property(e => e.Status)
            .HasColumnName("inbox_status_type_id");

        builder.Property(e => e.LogicalSourceName)
            .HasColumnName("logical_source_name");

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
