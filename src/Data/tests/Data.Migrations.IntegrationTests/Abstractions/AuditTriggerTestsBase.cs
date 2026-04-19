using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace EtherGizmos.Common.Abstractions;

[NonParallelizable]
public abstract class AuditTriggerTestsBase
{
    protected abstract string ConnectionString { get; }
    protected abstract DbConnection CreateConnection(string connectionString);
    protected abstract IServiceProvider CreateMigrationServices(string connectionString);

    protected abstract string CreateTableSql(string tableName);
    protected abstract string DropTableSql(string tableName);
    protected abstract string InsertRowSql(string tableName);
    protected abstract string UpdateRowSql(string tableName);
    protected abstract string SelectModifiedAtSql(string tableName);

    [Test]
    public async Task AuditTrigger_OnInsertAndUpdate_ShouldSetModifiedAtUtc()
    {
        var random = $"{Guid.NewGuid():N}";
        var tableName = $"audit_trigger_it_{random[..8]}";

        AuditTriggerTestMigration.TableName = tableName;

        await using var connection = CreateConnection(ConnectionString);
        await connection.OpenAsync();

        try
        {
            await ExecuteNonQueryAsync(connection, DropTableSql(tableName), ignoreFailure: true);
            await ExecuteNonQueryAsync(connection, CreateTableSql(tableName));

            var services = CreateMigrationServices(ConnectionString);
            using (var scope = services.CreateScope())
            {
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                runner.MigrateUp();
            }

            var beforeInsert = DateTime.UtcNow;
            await ExecuteNonQueryAsync(connection, InsertRowSql(tableName));
            var afterInsert = DateTime.UtcNow;

            var insertedModifiedAt = await ExecuteScalarDateTimeAsync(connection, SelectModifiedAtSql(tableName));

            Assert.That(insertedModifiedAt, Is.Not.Null);
            Assert.That(insertedModifiedAt!.Value.Kind == DateTimeKind.Utc || insertedModifiedAt.Value.Kind == DateTimeKind.Unspecified, Is.True);
            Assert.That(insertedModifiedAt.Value, Is.GreaterThanOrEqualTo(beforeInsert.AddMinutes(-1)));
            Assert.That(insertedModifiedAt.Value, Is.LessThanOrEqualTo(afterInsert.AddMinutes(1)));

            await Task.Delay(1200);

            await ExecuteNonQueryAsync(connection, UpdateRowSql(tableName));
            var updatedModifiedAt = await ExecuteScalarDateTimeAsync(connection, SelectModifiedAtSql(tableName));

            Assert.That(updatedModifiedAt, Is.Not.Null);
            Assert.That(updatedModifiedAt!.Value, Is.GreaterThan(insertedModifiedAt.Value));

            services = CreateMigrationServices(ConnectionString);
            using (var scope = services.CreateScope())
            {
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                runner.MigrateDown(0);
            }
        }
        finally
        {
            await ExecuteNonQueryAsync(connection, DropTableSql(tableName), ignoreFailure: true);
        }
    }

    private static async Task ExecuteNonQueryAsync(
        DbConnection connection,
        string sql,
        bool ignoreFailure = false)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch when (ignoreFailure)
        {
        }
    }

    private static async Task<DateTime?> ExecuteScalarDateTimeAsync(
        DbConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync();

        if (result is null || result is DBNull)
            return null;

        return result switch
        {
            DateTime dt => dt,
            DateTimeOffset dto => dto.UtcDateTime,
            _ => Convert.ToDateTime(result)
        };
    }
}
