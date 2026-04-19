namespace EtherGizmos.Common.Configuration;

internal class MessagingOptionsTests
{
    [Test]
    public void AddMap_WhenCalled_ShouldCreateMap()
    {
        //Arrange
        var options = new MessagingOptions();

        //Act
        options.TypeMappings.AddMap(typeof(TestMessage), "TestMessage");
        options.Build();

        //Assert
        var toString = options.ConvertType(typeof(TestMessage));
        var toType = options.ConvertType("TestMessage");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toString, Is.EqualTo("TestMessage"));
            Assert.That(toType, Is.EqualTo(typeof(TestMessage)));
        }
    }

    [Test]
    public void AddQueue_WhenCalled_ShouldAddQueue()
    {
        //Arrange
        var options = new MessagingOptions();

        //Act
        options.Publishers.AddQueue("q1", "my.queue");
        options.Listeners.AddQueue("q1", "my.queue");

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.Publishers["q1"].Name, Is.EqualTo("my.queue"));
            Assert.That(options.Publishers["q1"].IsTopic, Is.False);
            Assert.That(options.Listeners["q1"].Name, Is.EqualTo("my.queue"));
            Assert.That(options.Listeners["q1"].IsTopic, Is.False);
        }
    }

    [Test]
    public void AddTopic_WhenCalled_ShouldAddTopic()
    {
        //Arrange
        var options = new MessagingOptions();

        //Act
        options.Publishers.AddTopic("q1", "my.queue");
        options.Listeners.AddTopic("q1", "my.queue", "my.sub");

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.Publishers["q1"].Name, Is.EqualTo("my.queue"));
            Assert.That(options.Publishers["q1"].IsTopic, Is.True);
            Assert.That(options.Listeners["q1"].Name, Is.EqualTo("my.queue"));
            Assert.That(options.Listeners["q1"].IsTopic, Is.True);
            Assert.That(options.Listeners["q1"].Subscription, Is.EqualTo("my.sub"));
        }
    }

    [Test]
    public void ConvertType_WhenAutomaticAndUnmapped_ShouldNotThrow()
    {
        //Arrange
        var options = new MessagingOptions();

        //Act
        options.Build();

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.DoesNotThrow(() =>
            {
                options.ConvertType(typeof(InvalidMessage));
            });

            Assert.DoesNotThrow(() =>
            {
                options.ConvertType("EtherGizmos.Common.Configuration.MessagingOptionsTests+InvalidMessage, EtherGizmos.Common.Messaging.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            });
        }
    }

    [Test]
    public void ConvertType_WhenManualAndUnmapped_ShouldThrowInvalidOperationException()
    {
        //Arrange
        var options = new MessagingOptions();

        //Act
        options.TypeMappings.AddMap(typeof(TestMessage), "TestMessage");
        options.Build();

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                options.ConvertType(typeof(InvalidMessage));
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                options.ConvertType("InvalidMessage");
            });
        }
    }

    private class TestMessage { }

    private class InvalidMessage { }
}
