using EtherGizmos.Common.Abstractions;
using Moq;

namespace EtherGizmos.Common.Services;

internal class NotificationCollectorHostedServiceTests
{
    private Lazy<NotificationCollectorHostedService> _service;
    private List<Mock<INotificationCollector>> _collectorMocks;

    [SetUp]
    public void SetUp()
    {
        _collectorMocks = [];

        _collectorMocks.Add(new());

        _service = new(() => new(
            _collectorMocks.Select(e => e.Object)));
    }

    [Test]
    public async Task StartAsync_WhenCalled_ShouldStartAllCollectorsWithLinkedCancellationToken()
    {
        //Arrange
        _collectorMocks.Add(new());

        CancellationToken token0 = default;
        _collectorMocks[0].Setup(@interface =>
            @interface.CollectAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token =>
            {
                token0 = token;
            });

        CancellationToken token1 = default;
        _collectorMocks[1].Setup(@interface =>
            @interface.CollectAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token =>
            {
                token1 = token;
            });

        var service = _service.Value;

        using var cts = new CancellationTokenSource();

        //Act
        await service.StartAsync(cts.Token);

        //Assert
        _collectorMocks[0].Verify(e => e.CollectAsync(It.IsAny<CancellationToken>()), Times.Once());
        _collectorMocks[1].Verify(e => e.CollectAsync(It.IsAny<CancellationToken>()), Times.Once());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(token0.IsCancellationRequested, Is.False);
            Assert.That(token1.IsCancellationRequested, Is.False);
        }

        cts.Cancel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(token0.IsCancellationRequested, Is.True);
            Assert.That(token1.IsCancellationRequested, Is.True);
        }
    }

    [Test]
    public async Task StopAsync_WhenCalled_ShouldStopAllCollectorsByCancellingToken()
    {
        //Arrange
        CancellationToken token0 = default;
        _collectorMocks[0].Setup(@interface =>
            @interface.CollectAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token =>
            {
                token0 = token;
            });

        var service = _service.Value;

        await service.StartAsync(CancellationToken.None);

        //Act
        await service.StopAsync(CancellationToken.None);

        //Assert
        _collectorMocks[0].Verify(e => e.CollectAsync(It.IsAny<CancellationToken>()), Times.Once());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(token0.IsCancellationRequested, Is.True);
        }
    }
}
