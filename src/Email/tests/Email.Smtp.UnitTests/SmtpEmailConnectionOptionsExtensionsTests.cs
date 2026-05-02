using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common;

internal class SmtpEmailConnectionOptionsExtensionsTests
{
    [Test]
    public void IsPostgreSql_WhenPostgreSql_ShouldReturnTrue()
    {
        //Arrange
        var options = new SmtpEmailOptions();

        //Act
        var result = options.IsSmtp(out var smtp);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(smtp, Is.Not.Null);
        }
    }

    [Test]
    public void IsPostgreSql_WhenNotPostgreSql_ShouldReturnFalse()
    {
        //Arrange
        var options = new EmailConnectionOptions();

        //Act
        var result = options.IsSmtp(out var smtp);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(smtp, Is.Null);
        }
    }
}
