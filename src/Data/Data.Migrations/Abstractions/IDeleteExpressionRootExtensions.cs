using FluentMigrator;
using FluentMigrator.Builders.Delete;
using FluentMigrator.Builders.IfDatabase;
using FluentMigrator.Infrastructure;
using System.Reflection;

namespace EtherGizmos.Common.Abstractions;

public static class IDeleteExpressionRootExtensions
{
    public static void AuditTriggerV1(
        this IDeleteExpressionRoot @this,
        string table)
    {
        var context = (IMigrationContext)@this.GetType()
            .GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(@this)!;

        var mySql = new IfDatabaseExpressionRoot(context, ProcessorIdConstants.MySql);

        mySql.Execute
            .Sql($"drop trigger {MySqlHelper.Escape($"TR_{table}_audit_insert")};");

        mySql.Execute
            .Sql($"drop trigger {MySqlHelper.Escape($"TR_{table}_audit_update")};");

        var postgres = new IfDatabaseExpressionRoot(context, ProcessorIdConstants.PostgreSQL);

        postgres.Execute
            .Sql($"drop function {PostgreSqlHelper.Escape($"TR_{table}_audit")}() cascade;");

        var sqlServer = new IfDatabaseExpressionRoot(context, ProcessorIdConstants.SqlServer);

        sqlServer.Execute
            .Sql($"drop trigger {SqlServerHelper.Escape($"TR_{table}_audit")};");
    }
}
