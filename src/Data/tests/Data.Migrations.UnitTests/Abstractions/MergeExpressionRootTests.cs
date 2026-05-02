using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using Moq;

namespace EtherGizmos.Common.Abstractions;

internal class MergeExpressionRootTests
{
    [Test]
    public void IntoTable_WhenCalled_ShouldAddMergeExpressionToContext()
    {
        var expressions = new List<IMigrationExpression>();
        var context = new Mock<IMigrationContext>();
        context.SetupGet(x => x.Expressions).Returns(expressions);

        var root = new MergeExpressionRoot(context.Object);

        var builder = root.IntoTable("users");

        Assert.That(builder, Is.Not.Null);
        Assert.That(expressions.Count, Is.EqualTo(1));

        var expr = expressions.Single() as MergeDataExpression;
        Assert.That(expr, Is.Not.Null);
        Assert.That(expr!.TableName, Is.EqualTo("users"));
    }

    [Test]
    public void InSchema_WhenCalled_ShouldSetSchemaName()
    {
        var expression = new MergeDataExpression
        {
            TableName = "users"
        };

        var builder = new MergeDataExpressionStartBuilder(expression);

        var returned = builder.InSchema("app");

        Assert.That(returned, Is.SameAs(builder));
        Assert.That(expression.SchemaName, Is.EqualTo("app"));
    }

    [Test]
    public void Row_WhenFirstRow_ShouldCreateTypedBuilderAndAddRow()
    {
        var expression = new MergeDataExpression
        {
            TableName = "users"
        };

        var builder = new MergeDataExpressionStartBuilder(expression);

        var returned = builder.Row(new TestRow { Id = 1, Name = "Ryan" });

        Assert.That(returned, Is.Not.Null);
        Assert.That(expression.Rows.Count, Is.EqualTo(1));
        Assert.That(expression.Rows[0].Single(x => x.Key == "Id").Value, Is.EqualTo(1));
        Assert.That(expression.Rows[0].Single(x => x.Key == "Name").Value, Is.EqualTo("Ryan"));
    }

    [Test]
    public void TypedRow_WhenNull_ShouldThrowArgumentNullException()
    {
        var expression = new MergeDataExpression
        {
            TableName = "users"
        };

        var builder = new MergeDataExpressionTypedBuilder<TestRow>(expression);

        Assert.Throws<ArgumentNullException>(() => builder.Row(null!));
    }

    [Test]
    public void TypedRow_WhenValid_ShouldAddAdditionalRow()
    {
        var expression = new MergeDataExpression
        {
            TableName = "users"
        };

        var builder = new MergeDataExpressionTypedBuilder<TestRow>(expression);

        builder.Row(new TestRow { Id = 1, Name = "Ryan" });
        builder.Row(new TestRow { Id = 2, Name = "Duffy" });

        Assert.That(expression.Rows.Count, Is.EqualTo(2));
        Assert.That(expression.Rows[1].Single(x => x.Key == "Id").Value, Is.EqualTo(2));
        Assert.That(expression.Rows[1].Single(x => x.Key == "Name").Value, Is.EqualTo("Duffy"));
    }

    [Test]
    public void Match_WhenCalled_ShouldAddMatchColumnNamesFromProjectionType()
    {
        var expression = new MergeDataExpression
        {
            TableName = "users"
        };

        var builder = new MergeDataExpressionTypedBuilder<TestRow>(expression);

        builder.Match(x => new MatchProjection { Id = x.Id, Name = x.Name });

        Assert.That(expression.MatchColumns, Is.EquivalentTo(["Id", "Name"]));
    }

    private sealed class TestRow
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class MatchProjection
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
