using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Model;
using Moq;
using System.Data;

namespace EtherGizmos.Common.Abstractions;

internal class MergeDataExpressionTests
{
    [Test]
    public void ExecuteWith_WhenMatchingRowExists_ShouldPerformUpdate()
    {
        var processor = new Mock<IMigrationProcessor>();

        var existing = CreateDataSet(
            ("users", new[]
            {
                new DataColumn("Id", typeof(int)),
                new DataColumn("Name", typeof(string)),
                new DataColumn("Age", typeof(int))
            },
            new object[][]
            {
                [1, "Ryan", 20]
            }));

        processor.Setup(x => x.ReadTableData("app", "users")).Returns(existing);

        var expression = new MergeDataExpression
        {
            SchemaName = "app",
            TableName = "users"
        };

        expression.MatchColumns.Add("Id");
        expression.Rows.Add(new InsertionDataDefinition
        {
            new("Id", 1),
            new("Name", "Ryan"),
            new("Age", 30)
        });

        expression.ExecuteWith(processor.Object);

        processor.Verify(x => x.Process(It.Is<UpdateDataExpression>(e =>
            e.SchemaName == "app" &&
            e.TableName == "users" &&
            e.IsAllRows == false &&
            e.Where.Count == 1 &&
            e.Where.Single(w => w.Key == "Id").Value!.Equals(1) &&
            e.Set.Count == 2 &&
            e.Set.Any(s => s.Key == "Name" && Equals(s.Value, "Ryan")) &&
            e.Set.Any(s => s.Key == "Age" && Equals(s.Value, 30))
        )), Times.Once);

        processor.Verify(x => x.Process(It.IsAny<InsertDataExpression>()), Times.Never);
    }

    [Test]
    public void ExecuteWith_WhenMatchingRowDoesNotExist_ShouldPerformInsert()
    {
        var processor = new Mock<IMigrationProcessor>();

        var existing = CreateDataSet(
            ("users", new[]
            {
                new DataColumn("Id", typeof(int)),
                new DataColumn("Name", typeof(string))
            },
            new object[][]
            {
                [5, "Someone Else"]
            }));

        processor.Setup(x => x.ReadTableData(null, "users")).Returns(existing);

        var expression = new MergeDataExpression
        {
            TableName = "users"
        };

        expression.AdditionalFeatures["feature-a"] = "value-a";
        expression.MatchColumns.Add("Id");
        expression.Rows.Add(new InsertionDataDefinition
        {
            new("Id", 1),
            new("Name", "Ryan")
        });

        expression.ExecuteWith(processor.Object);

        processor.Verify(x => x.Process(It.Is<InsertDataExpression>(e =>
            e.SchemaName == null &&
            e.TableName == "users" &&
            e.Rows.Count == 1 &&
            e.Rows[0].Any(r => r.Key == "Id" && Equals(r.Value, 1)) &&
            e.Rows[0].Any(r => r.Key == "Name" && Equals(r.Value, "Ryan")) &&
            e.AdditionalFeatures.ContainsKey("feature-a") &&
            Equals(e.AdditionalFeatures["feature-a"], "value-a")
        )), Times.Once);

        processor.Verify(x => x.Process(It.IsAny<UpdateDataExpression>()), Times.Never);
    }

    [Test]
    public void ExecuteWith_WhenMatchColumnContainsNulls_ShouldStillMatchCorrectly()
    {
        var processor = new Mock<IMigrationProcessor>();

        var existing = CreateDataSet(
            ("users", new[]
            {
                new DataColumn("Code", typeof(string)),
                new DataColumn("Name", typeof(string))
            },
            new object[][]
            {
                [DBNull.Value, "Ryan"]
            }));

        processor.Setup(x => x.ReadTableData(null, "users")).Returns(existing);

        var expression = new MergeDataExpression
        {
            TableName = "users"
        };

        expression.MatchColumns.Add("Code");
        expression.Rows.Add(new InsertionDataDefinition
        {
            new("Code", DBNull.Value),
            new("Name", "Updated")
        });

        expression.ExecuteWith(processor.Object);

        processor.Verify(x => x.Process(It.IsAny<UpdateDataExpression>()), Times.Once);
        processor.Verify(x => x.Process(It.IsAny<InsertDataExpression>()), Times.Never);
    }

    private static DataSet CreateDataSet((string tableName, DataColumn[] columns, object[][] rows) table)
    {
        var dataSet = new DataSet();
        var dataTable = new DataTable(table.tableName);

        foreach (var column in table.columns)
        {
            dataTable.Columns.Add(column);
        }

        foreach (var row in table.rows)
        {
            dataTable.Rows.Add(row);
        }

        dataSet.Tables.Add(dataTable);
        return dataSet;
    }
}
