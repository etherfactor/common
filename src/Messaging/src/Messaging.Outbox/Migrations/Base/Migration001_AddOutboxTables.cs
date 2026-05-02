using EtherGizmos.Common.Abstractions;
using FluentMigrator;

namespace EtherGizmos.Common.Migrations.Base;

[CreatedAt(year: 2025, month: 12, day: 21, hour: 23, minute: 00, description: "Create outbox tables")]
public class Migration001_AddOutboxTables : AutoReversingMigration
{
    public override void Up()
    {
        /*
         * Create [dbo].[outbox_status_types]
         */
        Create.Table("outbox_status_types")
            .WithColumn("outbox_status_type_id").AsInt32().PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable();

        /*
         * Create [dbo].[outbox]
         */
        Create.Table("outbox")
            .WithColumn("outbox_id").AsInt32().PrimaryKey().Identity()
            .WithColumn("message_id").AsString(100).NotNullable()
            .WithColumn("queued_at_utc").AsDateTime2().NotNullable()
            .WithColumn("available_at_utc").AsDateTime2().NotNullable()
            .WithColumn("published_at_utc").AsDateTime2().Nullable()
            .WithColumn("outbox_status_type_id").AsInt32().NotNullable()
            .WithColumn("logical_destination_name").AsString(100).NotNullable()
            .WithColumn("type").AsString(1024).NotNullable()
            .WithColumn("payload").AsString(int.MaxValue).Nullable()
            .WithColumn("headers").AsString(int.MaxValue).NotNullable()
            .WithColumn("attempt_count").AsInt32().NotNullable()
            .WithColumn("last_attempt_at_utc").AsDateTime2().Nullable()
            .WithColumn("last_error").AsString(int.MaxValue).Nullable()
            .WithColumn("lock_id").AsGuid().Nullable()
            .WithColumn("locked_by").AsString(100).Nullable()
            .WithColumn("locked_until_utc").AsDateTime2().Nullable();

        Create.Index("IX_outbox_message_id")
            .OnTable("outbox")
            .OnColumn("message_id")
            .Unique();

        Create.Index("IX_outbox_lock_id")
            .OnTable("outbox")
            .OnColumn("lock_id")
            .Ascending();

        Create.ForeignKey("FK_outbox_outbox_status_type_id")
            .FromTable("outbox").ForeignColumn("outbox_status_type_id")
            .ToTable("outbox_status_types").PrimaryColumn("outbox_status_type_id");
    }
}
