using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Migrations.Base;

[CreatedAt(year: 2025, month: 02, day: 19, hour: 18, minute: 00, description: "Load notification tables")]
public class Migration002_LoadNotificationTables : MigrationExtension
{
    public override void Up()
    {
        /*
         * Load [dbo].[notification_status_types]
         */
        Merge.IntoTable("notification_status_types")
            .Row(new { notification_status_type_id = 1, name = "Pending", description = "This notification is waiting to be processed." })
            .Row(new { notification_status_type_id = 10, name = "In flight", description = "This notification is actively being processed." })
            .Row(new { notification_status_type_id = 100, name = "Sent", description = "This notification has been sent. No guarantee is made regarding its delivery." })
            .Row(new { notification_status_type_id = -100, name = "Failed", description = "This notification failed to process." })
            .Match(e => new { e.notification_status_type_id });

        // TODO: DELETE THIS
        Insert.IntoTable("notification_subscriptions")
            .Row(new { user_id = "ABC", event_type = "test.domain.event", channel_key = "webhook", schedule_type = "immediate", schedule_config = null as string, is_enabled = true })
            .Row(new { user_id = "ABC", event_type = "test.domain.event", channel_key = "webhook", schedule_type = "digest", schedule_config = "{\"cronExpression\":\"* * * * *\"}", is_enabled = true })
            .Row(new { user_id = "DEF", event_type = "test.domain.event", channel_key = "webhook", schedule_type = "immediate", schedule_config = null as string, is_enabled = true });
    }

    public override void Down() { }
}
