namespace EtherGizmos.Common.Abstractions;

internal class OneWayMigrationTests
{
    [Test]
    public void Down_WhenCalled_ShouldThrowInvalidOperationException()
    {
        var migration = new TestOneWayMigration();

        var ex = Assert.Throws<InvalidOperationException>(() => migration.Down());

        Assert.That(ex!.Message, Is.EqualTo("This migration cannot be reversed. To downgrade, a SQL restore must be performed."));
    }

    private sealed class TestOneWayMigration : OneWayMigration
    {
        public override void Up()
        {
        }
    }
}
