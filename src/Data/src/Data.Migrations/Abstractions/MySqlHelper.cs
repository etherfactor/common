using System.Data;

namespace EtherGizmos.Common.Abstractions;

internal static class MySqlHelper
{
    public static string Escape(string input)
    {
        return $"`{input.Replace("`", "``")}`".ToLowerInvariant();
    }

    public static string ToDbString(this DbType @this)
    {
        return @this switch
        {
            DbType.AnsiString => "varchar",
            DbType.AnsiStringFixedLength => "char(8000)",
            DbType.Boolean => "bit",
            DbType.Byte => "tinyint",
            DbType.Date => "date",
            DbType.DateTime => "datetime",
            DbType.DateTime2 => "datetime",
            DbType.DateTimeOffset => "datetime",
            DbType.Decimal => "decimal",
            DbType.Double => "double",
            DbType.Guid => "char(36)",
            DbType.Int16 => "smallint",
            DbType.Int32 => "int",
            DbType.Int64 => "bigint",
            DbType.String => "nvarchar",
            DbType.StringFixedLength => "nchar(8000)",
            DbType.Time => "time",
            _ => throw new NotImplementedException(),
        };
    }
}
