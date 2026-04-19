using EtherGizmos.Common.Abstractions;
using FluentMigrator;
using System.Data;

namespace EtherGizmos.Common;

[CreatedAt(year: 2026, month: 04, day: 05, hour: 12, minute: 00, description: "Audit trigger test migration", trackingId: 1)]
public sealed class AuditTriggerTestMigration : Migration
{
    public static string TableName { get; set; } = null!;

    public override void Up()
    {
        if (string.IsNullOrWhiteSpace(TableName))
            throw new InvalidOperationException("AuditTriggerTestMigration.TableName was not set.");

        Create.AuditTriggerV1(TableName, ("id", DbType.Int32));
    }

    public override void Down()
    {
        if (string.IsNullOrWhiteSpace(TableName))
            throw new InvalidOperationException("AuditTriggerTestMigration.TableName was not set.");

        Delete.AuditTriggerV1(TableName);
    }
}
