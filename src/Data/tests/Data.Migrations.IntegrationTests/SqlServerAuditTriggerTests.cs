using EtherGizmos.Common.Abstractions;
using FluentMigrator.Runner;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace EtherGizmos.Common;

internal class SqlServerAuditTriggerTests : AuditTriggerTestsBase
{
    protected override string ConnectionString => Setup.MsSqlConnectionString;

    protected override DbConnection CreateConnection(string connectionString)
        => new SqlConnection(connectionString);

    protected override IServiceProvider CreateMigrationServices(string connectionString)
    {
        return new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSqlServer2016()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(AuditTriggerTestMigration).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);
    }

    protected override string CreateTableSql(string tableName) => $@"
create table [{tableName}]
(
    [id] int not null primary key,
    [name] nvarchar(100) null,
    [modified_at_utc] datetime2(6) null
);";

    protected override string DropTableSql(string tableName) => $@"
if object_id(N'[{tableName}]', N'U') is not null
    drop table [{tableName}];";

    protected override string InsertRowSql(string tableName) => $@"
insert into [{tableName}] ([id], [name], [modified_at_utc])
values (1, N'initial', null);";

    protected override string UpdateRowSql(string tableName) => $@"
update [{tableName}]
set [name] = N'updated'
where [id] = 1;";

    protected override string SelectModifiedAtSql(string tableName) => $@"
select [modified_at_utc]
from [{tableName}]
where [id] = 1;";
}
