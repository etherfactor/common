using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Migrations.Base;

[CreatedAt(year: 2025, month: 12, day: 22, hour: 11, minute: 30, "Load inbox tables")]
public class Migration002_LoadInboxTables : MigrationExtension
{
    public override void Up()
    {
        /*
         * Load [dbo].[inbox_status_types]
         */
        Merge.IntoTable("inbox_status_types")
            .Row(new { inbox_status_type_id = 1, name = "Pending", description = "This message is waiting to be processed." })
            .Row(new { inbox_status_type_id = 10, name = "In flight", description = "This message is actively being processed." })
            .Row(new { inbox_status_type_id = 100, name = "Processed", description = "This message has successfully been processed." })
            .Row(new { inbox_status_type_id = -100, name = "Failed", description = "This message failed to process." })
            .Match(e => new { e.inbox_status_type_id });
    }

    public override void Down() { }
}
