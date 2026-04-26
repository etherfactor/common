using Microsoft.Extensions.Logging;
using Moq;

namespace EtherGizmos.Common.Abstractions;

internal class NotificationCollectorTests
{
    private Lazy<TestableNotificationCollector> _collector;
    private Mock<ILogger<TestableNotificationCollector>> _loggerMock;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new();

        _collector = new(() => new(
            _loggerMock.Object));
    }

    [Test]
    public async Task CollectAsync_WhenCancellationRequestedBeforeStart_ShouldNotCollectBatch()
    {
        //Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        //Act
        await _collector.Value.CollectAsync(cts.Token);

        //Assert
        Assert.That(_collector.Value.CollectBatchCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task CollectAsync_WhenCollectBatchCompletes_ShouldRepeatUntilCancelled()
    {
        //Arrange
        using var cts = new CancellationTokenSource();

        _collector.Value.UseDelay = TimeSpan.Zero;
        _collector.Value.OnCollectBatchAsync = () =>
        {
            if (_collector.Value.CollectBatchCallCount >= 3)
            {
                cts.Cancel();
            }

            return Task.CompletedTask;
        };

        //Act
        try
        {
            await _collector.Value.CollectAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        //Assert
        Assert.That(_collector.Value.CollectBatchCallCount, Is.EqualTo(3));
    }

    [Test]
    public async Task CollectAsync_WhenCollectBatchThrows_ShouldLogErrorAndContinueUntilCancelled()
    {
        //Arrange
        using var cts = new CancellationTokenSource();

        _collector.Value.UseDelay = TimeSpan.Zero;
        _collector.Value.Throw = true;
        _collector.Value.OnCollectBatchAsync = () =>
        {
            if (_collector.Value.CollectBatchCallCount >= 3)
            {
                cts.Cancel();
            }

            return Task.CompletedTask;
        };

        //Act
        try
        {
            await _collector.Value.CollectAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        //Assert
        Assert.That(_collector.Value.CollectBatchCallCount, Is.EqualTo(3));

        VerifyLogged(
            _loggerMock,
            LogLevel.Error,
            "Encountered an error while collecting notifications",
            Times.Exactly(3));
    }

    private static void VerifyLogged(
        Mock<ILogger<TestableNotificationCollector>> logger,
        LogLevel level,
        string containsMessage,
        Times times)
    {
        logger.Verify(@interface =>
            @interface.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(containsMessage, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    public class TestableNotificationCollector : NotificationCollector
    {
        public bool Throw { get; set; } = false;

        public TimeSpan UseDelay { get; set; } = TimeSpan.Zero;

        public override TimeSpan Delay => UseDelay;

        public int CollectBatchCallCount { get; set; } = 0;

        public Func<Task>? OnCollectBatchAsync { get; set; }

        public TestableNotificationCollector(
            ILogger<TestableNotificationCollector> logger) : base(logger) { }

        protected override async Task CollectBatchAsync(
            CancellationToken cancellationToken = default)
        {
            CollectBatchCallCount++;

            if (OnCollectBatchAsync is not null)
            {
                await OnCollectBatchAsync();
            }
            else
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            if (Throw) throw new Exception();
        }
    }
}
