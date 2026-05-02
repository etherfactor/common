using System.Data;

namespace EtherGizmos.Common.Abstractions;

internal static class PostgreSqlHelper
{
    public static string Escape(string input)
    {
        return $"\"{input.Replace("\"", "\"\"")}\"".ToLowerInvariant();
    }

    public static string ToDbString(this DbType @this)
    {
        return @this switch
        {
            DbType.AnsiString => "varchar",
            DbType.AnsiStringFixedLength => "char(8000)",
            DbType.Boolean => "boolean",
            DbType.Byte => "smallint",
            DbType.Date => "date",
            DbType.DateTime => "timestamp",
            DbType.DateTime2 => "timestamp",
            DbType.DateTimeOffset => "timestamp with time zone",
            DbType.Decimal => "numeric",
            DbType.Double => "double precision",
            DbType.Guid => "uuid",
            DbType.Int16 => "smallint",
            DbType.Int32 => "integer",
            DbType.Int64 => "bigint",
            DbType.String => "text",
            DbType.StringFixedLength => "char(8000)",
            DbType.Time => "time",
            _ => throw new NotImplementedException(),
        };
    }
}
