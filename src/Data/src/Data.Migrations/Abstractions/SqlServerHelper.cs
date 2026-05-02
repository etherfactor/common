using System.Data;

namespace EtherGizmos.Common.Abstractions;

internal static class SqlServerHelper
{
    public static string Escape(string input)
    {
        return $"[{input.Replace("]", "]]")}]";
    }

    public static string ToDbString(this DbType @this)
    {
        return @this switch
        {
            DbType.AnsiString => "varchar(max)",
            DbType.AnsiStringFixedLength => "char(8000)",
            DbType.Boolean => "bit",
            DbType.Byte => "tinyint",
            DbType.Date => "date",
            DbType.DateTime => "datetime",
            DbType.DateTime2 => "datetime2",
            DbType.DateTimeOffset => "datetimeoffset",
            DbType.Decimal => "decimal",
            DbType.Double => "float",
            DbType.Guid => "uniqueidentifier",
            DbType.Int16 => "smallint",
            DbType.Int32 => "int",
            DbType.Int64 => "bigint",
            DbType.String => "nvarchar(max)",
            DbType.StringFixedLength => "nchar(8000)",
            DbType.Time => "time",
            _ => throw new NotImplementedException(),
        };
    }
}
