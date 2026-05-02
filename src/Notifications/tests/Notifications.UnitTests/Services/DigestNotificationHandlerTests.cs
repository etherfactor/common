namespace EtherGizmos.Common.Services;

internal class DigestNotificationHandlerTests
{
    private DigestNotificationHandler _handler = new();

    [Test]
    public void HandleAsync_WhenCalled_ShouldNoOp()
    {
        //Act
        var task = _handler.HandleAsync(0);

        //Assert
        Assert.That(task, Is.EqualTo(Task.CompletedTask));
    }
}
