using System.Data;

namespace EtherGizmos.Common.Abstractions;

public class DatabaseHelperTests
{
    [TestCase("Table", "`table`")]
    [TestCase("TR_test`name", "`tr_test``name`")]
    public void MySql_Escape_ShouldReturnEscapedString(string input, string expected)
    {
        Assert.That(MySqlHelper.Escape(input), Is.EqualTo(expected));
    }

    [TestCase("Table", "\"table\"")]
    [TestCase("TR_test\"name", "\"tr_test\"\"name\"")]
    public void PostgreSql_Escape_ShouldReturnEscapedString(string input, string expected)
    {
        Assert.That(PostgreSqlHelper.Escape(input), Is.EqualTo(expected));
    }

    [TestCase("Table", "[Table]")]
    [TestCase("TR_test]name", "[TR_test]]name]")]
    public void SqlServer_Escape_ShouldReturnEscapedString(string input, string expected)
    {
        Assert.That(SqlServerHelper.Escape(input), Is.EqualTo(expected));
    }

    [Test]
    public void MySql_ToDbString_ShouldReturnExpectedMappings()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(MySqlHelper.ToDbString(DbType.AnsiString), Is.EqualTo("varchar"));
            Assert.That(MySqlHelper.ToDbString(DbType.AnsiStringFixedLength), Is.EqualTo("char(8000)"));
            Assert.That(MySqlHelper.ToDbString(DbType.Boolean), Is.EqualTo("bit"));
            Assert.That(MySqlHelper.ToDbString(DbType.Byte), Is.EqualTo("tinyint"));
            Assert.That(MySqlHelper.ToDbString(DbType.Date), Is.EqualTo("date"));
            Assert.That(MySqlHelper.ToDbString(DbType.DateTime), Is.EqualTo("datetime"));
            Assert.That(MySqlHelper.ToDbString(DbType.DateTime2), Is.EqualTo("datetime"));
            Assert.That(MySqlHelper.ToDbString(DbType.DateTimeOffset), Is.EqualTo("datetime"));
            Assert.That(MySqlHelper.ToDbString(DbType.Decimal), Is.EqualTo("decimal"));
            Assert.That(MySqlHelper.ToDbString(DbType.Double), Is.EqualTo("double"));
            Assert.That(MySqlHelper.ToDbString(DbType.Guid), Is.EqualTo("char(36)"));
            Assert.That(MySqlHelper.ToDbString(DbType.Int16), Is.EqualTo("smallint"));
            Assert.That(MySqlHelper.ToDbString(DbType.Int32), Is.EqualTo("int"));
            Assert.That(MySqlHelper.ToDbString(DbType.Int64), Is.EqualTo("bigint"));
            Assert.That(MySqlHelper.ToDbString(DbType.String), Is.EqualTo("nvarchar"));
            Assert.That(MySqlHelper.ToDbString(DbType.StringFixedLength), Is.EqualTo("nchar(8000)"));
            Assert.That(MySqlHelper.ToDbString(DbType.Time), Is.EqualTo("time"));
        }
    }

    [Test]
    public void PostgreSql_ToDbString_ShouldReturnExpectedMappings()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(PostgreSqlHelper.ToDbString(DbType.AnsiString), Is.EqualTo("varchar"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.AnsiStringFixedLength), Is.EqualTo("char(8000)"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.Boolean), Is.EqualTo("boolean"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.Byte), Is.EqualTo("smallint"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.Date), Is.EqualTo("date"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.DateTime), Is.EqualTo("timestamp"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.DateTime2), Is.EqualTo("timestamp"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.DateTimeOffset), Is.EqualTo("timestamp with time zone"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.Decimal), Is.EqualTo("numeric"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.Double), Is.EqualTo("double precision"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.Guid), Is.EqualTo("uuid"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.Int16), Is.EqualTo("smallint"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.Int32), Is.EqualTo("integer"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.Int64), Is.EqualTo("bigint"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.String), Is.EqualTo("text"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.StringFixedLength), Is.EqualTo("char(8000)"));
            Assert.That(PostgreSqlHelper.ToDbString(DbType.Time), Is.EqualTo("time"));
        }
    }

    [Test]
    public void SqlServer_ToDbString_ShouldReturnExpectedMappings()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(SqlServerHelper.ToDbString(DbType.AnsiString), Is.EqualTo("varchar(max)"));
            Assert.That(SqlServerHelper.ToDbString(DbType.AnsiStringFixedLength), Is.EqualTo("char(8000)"));
            Assert.That(SqlServerHelper.ToDbString(DbType.Boolean), Is.EqualTo("bit"));
            Assert.That(SqlServerHelper.ToDbString(DbType.Byte), Is.EqualTo("tinyint"));
            Assert.That(SqlServerHelper.ToDbString(DbType.Date), Is.EqualTo("date"));
            Assert.That(SqlServerHelper.ToDbString(DbType.DateTime), Is.EqualTo("datetime"));
            Assert.That(SqlServerHelper.ToDbString(DbType.DateTime2), Is.EqualTo("datetime2"));
            Assert.That(SqlServerHelper.ToDbString(DbType.DateTimeOffset), Is.EqualTo("datetimeoffset"));
            Assert.That(SqlServerHelper.ToDbString(DbType.Decimal), Is.EqualTo("decimal"));
            Assert.That(SqlServerHelper.ToDbString(DbType.Double), Is.EqualTo("float"));
            Assert.That(SqlServerHelper.ToDbString(DbType.Guid), Is.EqualTo("uniqueidentifier"));
            Assert.That(SqlServerHelper.ToDbString(DbType.Int16), Is.EqualTo("smallint"));
            Assert.That(SqlServerHelper.ToDbString(DbType.Int32), Is.EqualTo("int"));
            Assert.That(SqlServerHelper.ToDbString(DbType.Int64), Is.EqualTo("bigint"));
            Assert.That(SqlServerHelper.ToDbString(DbType.String), Is.EqualTo("nvarchar(max)"));
            Assert.That(SqlServerHelper.ToDbString(DbType.StringFixedLength), Is.EqualTo("nchar(8000)"));
            Assert.That(SqlServerHelper.ToDbString(DbType.Time), Is.EqualTo("time"));
        }
    }

    [Test]
    public void All_ToDbString_UnsupportedDbType_ShouldThrowInvalidOperationException()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<NotImplementedException>(() => MySqlHelper.ToDbString(DbType.Currency));
            Assert.Throws<NotImplementedException>(() => PostgreSqlHelper.ToDbString(DbType.Currency));
            Assert.Throws<NotImplementedException>(() => SqlServerHelper.ToDbString(DbType.Currency));
        }
    }
}
