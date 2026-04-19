using EtherGizmos.Common.Abstractions;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace EtherGizmos.Common;

internal class MySqlAuditTriggerTests : AuditTriggerTestsBase
{
    protected override string ConnectionString => Setup.MySqlConnectionString;

    protected override DbConnection CreateConnection(string connectionString)
        => new MySqlConnection(connectionString);

    protected override IServiceProvider CreateMigrationServices(string connectionString)
    {
        return new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddMySql5()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(AuditTriggerTestMigration).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);
    }

    protected override string CreateTableSql(string tableName) => $@"
create table `{tableName}`
(
    `id` int not null primary key,
    `name` varchar(100) null,
    `modified_at_utc` datetime(6) null
);";

    protected override string DropTableSql(string tableName) => $@"
drop table if exists `{tableName}`;";

    protected override string InsertRowSql(string tableName) => $@"
insert into `{tableName}` (`id`, `name`, `modified_at_utc`)
values (1, 'initial', null);";

    protected override string UpdateRowSql(string tableName) => $@"
update `{tableName}`
set `name` = 'updated'
where `id` = 1;";

    protected override string SelectModifiedAtSql(string tableName) => $@"
select `modified_at_utc`
from `{tableName}`
where `id` = 1;";
}
