using EtherGizmos.Common.Abstractions;
using FluentMigrator;

namespace EtherGizmos.Common.Migrations.Base;

[CreatedAt(year: 2026, month: 02, day: 19, hour: 17, minute: 30, description: "Create notification tables")]
public class Migration001_CreateNotificationTables : AutoReversingMigration
{
    public override void Up()
    {
        /*
         * Create [notification]
         */
        Create.Schema("notification");

        /*
         * Create [notification].[status_types]
         */
        Create.Table("status_types").InSchema("notification")
            .WithColumn("status_type_id").AsInt32().PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable();

        /*
         * Create [notification].[events]
         */
        Create.Table("events").InSchema("notification")
            .WithColumn("event_id").AsString(100).PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("is_available").AsBoolean().NotNullable()
            .WithColumn("last_seen_at_utc").AsDateTime2().NotNullable()
            .WithColumn("config_schema").AsString(int.MaxValue).NotNullable();

        /*
         * Create [notification].[channels]
         */
        Create.Table("channels").InSchema("notification")
            .WithColumn("channel_id").AsString(100).PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("is_available").AsBoolean().NotNullable()
            .WithColumn("last_seen_at_utc").AsDateTime2().NotNullable()
            .WithColumn("config_schema").AsString(int.MaxValue).NotNullable();

        /*
         * Create [notification].[schedules]
         */
        Create.Table("schedules").InSchema("notification")
            .WithColumn("schedule_id").AsString(100).PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("is_available").AsBoolean().NotNullable()
            .WithColumn("last_seen_at_utc").AsDateTime2().NotNullable()
            .WithColumn("config_schema").AsString(int.MaxValue).NotNullable();

        /*
         * Create [notification].[event_supports]
         */
        Create.Table("event_supports").InSchema("notification")
            .WithColumn("event_id").AsString(100).PrimaryKey()
            .WithColumn("channel_id").AsString(100).PrimaryKey()
            .WithColumn("schedule_id").AsString(100).PrimaryKey();

        Create.ForeignKey("FK_event_supports_event_id")
            .FromTable("event_supports").InSchema("notification").ForeignColumn("event_id")
            .ToTable("events").InSchema("notification").PrimaryColumn("event_id");

        Create.ForeignKey("FK_event_supports_channel_id")
            .FromTable("event_supports").InSchema("notification").ForeignColumn("channel_id")
            .ToTable("channels").InSchema("notification").PrimaryColumn("channel_id");

        Create.ForeignKey("FK_event_supports_schedule_id")
            .FromTable("event_supports").InSchema("notification").ForeignColumn("schedule_id")
            .ToTable("schedules").InSchema("notification").PrimaryColumn("schedule_id");

        /*
         * Create [notification].[subscriptions]
         */
        Create.Table("subscriptions").InSchema("notification")
            .WithColumn("subscription_id").AsInt64().PrimaryKey().Identity()
            .WithColumn("user_id").AsString(100).NotNullable()
            .WithColumn("event_id").AsString(100).NotNullable()
            .WithColumn("event_config").AsString(int.MaxValue).NotNullable()
            .WithColumn("channel_id").AsString(100).NotNullable()
            .WithColumn("channel_config").AsString(int.MaxValue).NotNullable()
            .WithColumn("schedule_id").AsString(100).NotNullable()
            .WithColumn("schedule_config").AsString(int.MaxValue).NotNullable()
            .WithColumn("is_enabled").AsBoolean().NotNullable()
            .WithColumn("last_notification_at").AsDateTime2().Nullable()
            .WithColumn("next_notification_at").AsDateTime2().Nullable();

        Create.Index("IX_subscriptions_user_id")
            .OnTable("subscriptions").InSchema("notification")
            .OnColumn("user_id").Ascending();

        Create.ForeignKey("FK_subscriptions_event_id")
            .FromTable("subscriptions").InSchema("notification").ForeignColumn("event_id")
            .ToTable("events").InSchema("notification").PrimaryColumn("event_id");

        Create.ForeignKey("FK_subscriptions_channel_id")
            .FromTable("subscriptions").InSchema("notification").ForeignColumn("channel_id")
            .ToTable("channels").InSchema("notification").PrimaryColumn("channel_id");

        Create.ForeignKey("FK_subscriptions_schedule_id")
            .FromTable("subscriptions").InSchema("notification").ForeignColumn("schedule_id")
            .ToTable("schedules").InSchema("notification").PrimaryColumn("schedule_id");

        /*
         * Create [notification].[instances]
         */
        Create.Table("instances").InSchema("notification")
            .WithColumn("instance_id").AsInt64().PrimaryKey().Identity()
            .WithColumn("occurrence_id").AsGuid().NotNullable()
            .WithColumn("created_at_utc").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("sent_at_utc").AsDateTime2().Nullable()
            .WithColumn("subscription_id").AsInt64().NotNullable()
            .WithColumn("event_id").AsString(100).NotNullable()
            .WithColumn("channel_id").AsString(100).NotNullable()
            .WithColumn("schedule_id").AsString(100).NotNullable()
            .WithColumn("is_derived").AsBoolean().NotNullable()
            .WithColumn("payload_type").AsString(int.MaxValue).NotNullable()
            .WithColumn("payload").AsString(int.MaxValue).NotNullable()
            .WithColumn("status_type_id").AsInt32().NotNullable()
            .WithColumn("headers").AsString(int.MaxValue).NotNullable()
            .WithColumn("attempt_count").AsInt32().NotNullable()
            .WithColumn("last_attempt_at_utc").AsDateTime2().Nullable()
            .WithColumn("last_error").AsString(int.MaxValue).Nullable()
            .WithColumn("lock_id").AsGuid().Nullable()
            .WithColumn("locked_by").AsString(100).Nullable()
            .WithColumn("locked_until_utc").AsDateTime2().Nullable();

        Create.Index("IX_instances_occurrence_id")
            .OnTable("instances").InSchema("notification")
            .OnColumn("occurrence_id").Ascending();

        Create.Index("IX_instances_created_at_utc")
            .OnTable("instances").InSchema("notification")
            .OnColumn("created_at_utc").Descending();

        Create.ForeignKey("FK_instances_subscription_id")
            .FromTable("instances").InSchema("notification").ForeignColumn("subscription_id")
            .ToTable("subscriptions").InSchema("notification").PrimaryColumn("subscription_id");

        Create.Index("IX_instances_subscription_id")
            .OnTable("instances").InSchema("notification")
            .OnColumn("subscription_id").Ascending();

        Create.ForeignKey("FK_instances_event_id")
            .FromTable("instances").InSchema("notification").ForeignColumn("event_id")
            .ToTable("events").InSchema("notification").PrimaryColumn("event_id");

        Create.Index("IX_instances_event_id_created_at_utc")
            .OnTable("instances").InSchema("notification")
            .OnColumn("event_id").Ascending()
            .OnColumn("created_at_utc").Descending();

        Create.ForeignKey("FK_instances_channel_id")
            .FromTable("instances").InSchema("notification").ForeignColumn("channel_id")
            .ToTable("channels").InSchema("notification").PrimaryColumn("channel_id");

        Create.Index("IX_instances_channel_id_created_at_utc")
            .OnTable("instances").InSchema("notification")
            .OnColumn("channel_id").Ascending()
            .OnColumn("created_at_utc").Descending();

        Create.ForeignKey("FK_instances_schedule_id")
            .FromTable("instances").InSchema("notification").ForeignColumn("schedule_id")
            .ToTable("schedules").InSchema("notification").PrimaryColumn("schedule_id");

        Create.ForeignKey("FK_instances_status_type_id")
            .FromTable("instances").InSchema("notification").ForeignColumn("status_type_id")
            .ToTable("status_types").InSchema("notification").PrimaryColumn("status_type_id");

        Create.Index("IX_instances_status_type_id_created_at_utc")
            .OnTable("instances").InSchema("notification")
            .OnColumn("status_type_id").Ascending()
            .OnColumn("created_at_utc").Descending();

        Create.Index("IX_instances_lock_id")
            .OnTable("instances").InSchema("notification")
            .OnColumn("lock_id").Ascending();
    }
}
