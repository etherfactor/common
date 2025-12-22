using EtherGizmos.Common.Abstractions;
using FluentMigrator;

namespace EtherGizmos.Common.Migrations;

[CreatedAt(year: 2025, month: 12, day: 22, hour: 11, minute: 00, description: "Create inbox tables")]
public class Migration001_CreateInboxTables : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("inbox")
            .WithColumn("inbox_id").AsInt32().PrimaryKey().Identity()
            .WithColumn("message_id").AsString(100).NotNullable()
            .WithColumn("received_at_utc").AsDateTime2().NotNullable()
            .WithColumn("processed_at_utc").AsDateTime2().Nullable()
            .WithColumn("subscription").AsString(100).NotNullable()
            .WithColumn("consumer_name").AsString(1024).NotNullable()
            .WithColumn("inbox_status_type_id").AsInt32().NotNullable()
            .WithColumn("logical_source_name").AsString(100).NotNullable()
            .WithColumn("type").AsString(1024).NotNullable()
            .WithColumn("payload").AsString(int.MaxValue)
            .WithColumn("headers").AsString(int.MaxValue)
            .WithColumn("attempt_count").AsInt32().NotNullable()
            .WithColumn("last_attempt_at_utc").AsDateTime2().Nullable()
            .WithColumn("last_error").AsString(int.MaxValue).Nullable()
            .WithColumn("lock_id").AsGuid().Nullable()
            .WithColumn("locked_by").AsString(100).Nullable()
            .WithColumn("locked_until_utc").AsDateTime2().Nullable();

        Create.Index("IX_inbox_message_id_subscription_consumer_name")
            .OnTable("inbox")
            .OnColumn("message_id").Unique()
            .OnColumn("subscription").Unique()
            .OnColumn("consumer_name").Unique();
    }
}
