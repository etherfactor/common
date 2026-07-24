using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Migrations.Base;

[CreatedAt(year: 2026, month: 02, day: 19, hour: 18, minute: 00, description: "Load notification tables")]
public class Migration002_LoadNotificationTables : MigrationExtension
{
    public override void Up()
    {
        /*
         * Load [notification].[status_types]
         */
        Merge.IntoTable("status_types").InSchema("notification")
            .Row(new { status_type_id = 1, name = "Pending", description = "This notification is waiting to be processed." })
            .Row(new { status_type_id = 10, name = "In flight", description = "This notification is actively being processed." })
            .Row(new { status_type_id = 100, name = "Sent", description = "This notification has been sent. No guarantee is made regarding its delivery." })
            .Row(new { status_type_id = -100, name = "Failed", description = "This notification failed to process." })
            .Match(e => new { e.status_type_id });
    }

    public override void Down() { }
}
