using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using Moq;
using System.Reflection;

namespace EtherGizmos.Common.Abstractions;

internal class MigrationExtensionTests
{
    [Test]
    public void Merge_WhenCalled_ShouldReturnRootThatUsesMigrationContextExpressions()
    {
        var expressions = new List<IMigrationExpression>();
        var context = new Mock<IMigrationContext>();
        context.SetupGet(x => x.Expressions).Returns(expressions);

        var migration = new TestMigrationExtension();

        var field = typeof(MigrationBase)
            .GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, "Could not find MigrationBase._context");
        field!.SetValue(migration, context.Object);

        migration.Merge
            .IntoTable("users")
            .Row(new { Id = 1, Name = "Ryan" })
            .Match(x => new { x.Id });

        Assert.That(expressions.Count, Is.EqualTo(1));
        var expr = expressions.Single() as MergeDataExpression;
        Assert.That(expr, Is.Not.Null);
        Assert.That(expr!.TableName, Is.EqualTo("users"));
        Assert.That(expr.Rows.Count, Is.EqualTo(1));
        Assert.That(expr.MatchColumns, Contains.Item("Id"));
    }

    private sealed class TestMigrationExtension : MigrationExtension
    {
        public override void Up()
        {
        }

        public override void Down()
        {
        }
    }
}
