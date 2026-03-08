using EtherGizmos.Common.Abstractions;
using FluentMigrator;

namespace EtherGizmos.Common.Migrations.Base;

[CreatedAt(year: 2025, month: 02, day: 19, hour: 17, minute: 30, description: "Create notification tables")]
public class Migration001_CreateNotificationTables : AutoReversingMigration
{
    public override void Up()
    {
        /*
         * Create [dbo].[notification_status_types]
         */
        Create.Table("notification_status_types")
            .WithColumn("notification_status_type_id").AsInt32().PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable();

        /*
         * Create [dbo].[notification_subscriptions]
         */
        Create.Table("notification_subscriptions")
            .WithColumn("notification_subscription_id").AsInt64().PrimaryKey().Identity()
            .WithColumn("user_id").AsString(100).NotNullable()
            .WithColumn("event_type").AsString(100).NotNullable()
            .WithColumn("channel_key").AsString(100).NotNullable()
            .WithColumn("schedule_type").AsString(100).NotNullable()
            .WithColumn("schedule_config").AsString(int.MaxValue).Nullable()
            .WithColumn("is_enabled").AsBoolean().NotNullable()
            .WithColumn("last_notification_at").AsDateTime2().Nullable()
            .WithColumn("next_notification_at").AsDateTime2().Nullable();

        /*
         * Create [dbo].[notifications]
         */
        Create.Table("notifications")
            .WithColumn("notification_id").AsInt64().PrimaryKey().Identity()
            .WithColumn("event_id").AsGuid().NotNullable()
            .WithColumn("created_at").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("sent_at").AsDateTime2().Nullable()
            .WithColumn("notification_subscription_id").AsInt64().NotNullable()
            .WithColumn("payload_type").AsString(int.MaxValue).NotNullable()
            .WithColumn("payload").AsString(int.MaxValue).NotNullable()
            .WithColumn("notification_status_type_id").AsInt32().NotNullable()
            .WithColumn("attempt_count").AsInt32().NotNullable();

        Create.Index("IX_notifications_event_id")
            .OnTable("notifications")
            .OnColumn("event_id")
            .Ascending();

        Create.ForeignKey("FK_notifications_notification_subscription_id")
            .FromTable("notifications").ForeignColumn("notification_subscription_id")
            .ToTable("notification_subscriptions").PrimaryColumn("notification_subscription_id");

        Create.Index("IX_notifications_notification_subscription_id")
            .OnTable("notifications")
            .OnColumn("notification_subscription_id")
            .Ascending();

        Create.ForeignKey("FK_notifications_notification_status_type_id")
            .FromTable("notifications").ForeignColumn("notification_status_type_id")
            .ToTable("notification_status_types").PrimaryColumn("notification_status_type_id");

        Create.Index("IX_notifications_notification_status_type_id")
            .OnTable("notifications")
            .OnColumn("notification_status_type_id")
            .Ascending();
    }
}
