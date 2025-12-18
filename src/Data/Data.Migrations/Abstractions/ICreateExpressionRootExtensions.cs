using FluentMigrator;
using FluentMigrator.Builders.Create;
using FluentMigrator.Builders.IfDatabase;
using FluentMigrator.Infrastructure;
using System.Data;
using System.Reflection;

namespace EtherGizmos.Common.Abstractions;

public static class ICreateExpressionRootExtensions
{
    public static void AuditTriggerV1(
        this ICreateExpressionRoot @this,
        string table,
        params (string Name, DbType Type)[] primaryKeys)
    {
        var context = (IMigrationContext)@this.GetType()
            .GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(@this)!;

        var mySql = new IfDatabaseExpressionRoot(context, ProcessorIdConstants.MySql);

        mySql.Execute
            .Sql($@"create trigger {MySqlHelper.Escape($"TR_{table}_audit_insert")}
before insert on {MySqlHelper.Escape(table)}
for each row
begin
    set new.`modified_at_utc` = utc_timestamp;
end;");

        mySql.Execute
            .Sql($@"create trigger {MySqlHelper.Escape($"TR_{table}_audit_update")}
before update on {MySqlHelper.Escape(table)}
for each row
begin
    set new.`modified_at_utc` = utc_timestamp;
end;");

        var postgres = new IfDatabaseExpressionRoot(context, ProcessorIdConstants.PostgreSQL);

        postgres.Execute
            .Sql($@"create or replace function {PostgreSqlHelper.Escape($"TR_{table}_audit")}()
returns trigger as $$
begin
    --Set the last modified time of the record
    new.""modified_at_utc"" := timezone('utc', now());
    return new;
end;
$$ language plpgsql;

create trigger {PostgreSqlHelper.Escape($"TR_{table}_audit")}
before insert or update
on {PostgreSqlHelper.Escape(table)}
for each row
execute function {PostgreSqlHelper.Escape($"TR_{table}_audit")}();");

        var sqlServer = new IfDatabaseExpressionRoot(context, ProcessorIdConstants.SqlServer);

        sqlServer.Execute
            .Sql($@"create trigger {SqlServerHelper.Escape($"TR_{table}_audit")}
on {SqlServerHelper.Escape(table)}
after insert, update
as
begin
    set nocount on;

    {string.Join(Environment.NewLine + "    ", primaryKeys.Select((key, i) => $"@RecordId{i} {SqlServerHelper.ToDbString(key.Type)};"))}

    --Get the id of the inserted record
    select {string.Join("," + Environment.NewLine + "      ", primaryKeys.Select((key, i) => $"@RecordId{i} = inserted.{SqlServerHelper.Escape(key.Name)}"))}
      from inserted;

    --Set the last modified time of the record
    update {SqlServerHelper.Escape(table)}
      set [modified_at_utc] = getutcdate()
      where {string.Join(Environment.NewLine + "        and ", primaryKeys.Select((key, i) => $"{SqlServerHelper.Escape(key.Name)} = @RecordId{i}"))}
end;");
    }
}
