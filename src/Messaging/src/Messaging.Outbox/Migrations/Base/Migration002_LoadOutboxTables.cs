using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Migrations.Base;

[CreatedAt(year: 2025, month: 12, day: 21, hour: 23, minute: 30, description: "Load outbox tables")]
public class Migration002_LoadOutboxTables : MigrationExtension
{
    public override void Up()
    {
        /*
         * Load [dbo].[outbox_status_types]
         */
        Merge.IntoTable("outbox_status_types")
            .Row(new { outbox_status_type_id = 1, name = "Pending", description = "This message is waiting to be published." })
            .Row(new { outbox_status_type_id = 10, name = "In flight", description = "This message is actively being published." })
            .Row(new { outbox_status_type_id = 100, name = "Published", description = "This message has successfully been published." })
            .Row(new { outbox_status_type_id = -100, name = "Failed", description = "This message failed to publish." })
            .Match(e => new { e.outbox_status_type_id });
    }

    public override void Down() { }
}
