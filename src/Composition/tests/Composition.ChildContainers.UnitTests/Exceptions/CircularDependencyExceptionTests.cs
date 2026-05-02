namespace EtherGizmos.Common.Exceptions;

internal class CircularDependencyExceptionTests
{
    [Test]
    public void Constructor_SpecifiesDependencies_CanGet()
    {
        //Arrange
        var exception = new CircularDependencyException([typeof(object), typeof(string), typeof(object)]);

        //Act

        //Assert
        Assert.That(exception.DependencyChain, Is.Not.Null);
        Assert.That(exception.DependencyChain.Count(), Is.EqualTo(3));
    }
}
