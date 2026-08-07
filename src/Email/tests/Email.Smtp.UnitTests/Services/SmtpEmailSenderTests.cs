using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using MimeKit;
using Moq;

namespace EtherGizmos.Common.Services;

internal class SmtpEmailSenderTests
{
    [Test]
    public void BuildMimeMessage_WhenNotEmpty_ShouldSetFrom()
    {
        var message = new EmailMessage
        {
            From = new EmailAddress("sender@test.com", "Sender"),
            Subject = "Hello",
        };

        var mime = SmtpEmailSender.BuildMimeMessage(new(), message);

        Assert.That(mime.From, Has.Count.EqualTo(1));
        var from = (MailboxAddress)mime.From[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(from.Name, Is.EqualTo("Sender"));
            Assert.That(from.Address, Is.EqualTo("sender@test.com"));
        }
    }

    [Test]
    public void BuildMimeMessage_WhenEmptyAddress_ShouldOmit()
    {
        var message = new EmailMessage
        {
            From = EmailAddress.Empty,
            To =
            [
                EmailAddress.Empty,
                new EmailAddress("User", "user@test.com"),
            ],
            Cc =
            [
                EmailAddress.Empty,
                new EmailAddress("Other", "other@test.com"),
            ],
            Bcc =
            [
                new EmailAddress("Hidden", "hidden@test.com"),
                EmailAddress.Empty,
            ],
            Subject = "Hello",
        };

        var mime = SmtpEmailSender.BuildMimeMessage(new(), message);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mime.From, Is.Empty);
            Assert.That(mime.To, Has.Count.EqualTo(1));
            Assert.That(mime.Cc, Has.Count.EqualTo(1));
            Assert.That(mime.Bcc, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void BuildMimeMessage_WhenHtmlBodyPresent_ShouldUseHtml()
    {
        var message = new EmailMessage
        {
            Subject = "Hello",
            TextBody = "plain",
            HtmlBody = "<b>html</b>",
        };

        var mime = SmtpEmailSender.BuildMimeMessage(new(), message);

        Assert.That(mime.Body, Is.TypeOf<TextPart>());
        var body = (TextPart)mime.Body;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(body.IsHtml, Is.True);
            Assert.That(body.Text, Is.EqualTo("<b>html</b>"));
        }
    }

    [Test]
    public void BuildMimeMessage_WhenHtmlBodyMissing_ShouldUsePlainText()
    {
        var message = new EmailMessage
        {
            Subject = "Hello",
            TextBody = "plain",
        };

        var mime = SmtpEmailSender.BuildMimeMessage(new(), message);

        var body = (TextPart)mime.Body!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(body.IsHtml, Is.False);
            Assert.That(body.Text, Is.EqualTo("plain"));
        }
    }

    [Test]
    public void BuildMimeMessage_WhenBothBodiesMissing_ShouldUseEmptyPlainText()
    {
        var message = new EmailMessage
        {
            Subject = "Hello",
        };

        var mime = SmtpEmailSender.BuildMimeMessage(new(), message);

        var body = (TextPart)mime.Body!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(body.IsHtml, Is.False);
            Assert.That(body.Text, Is.EqualTo(string.Empty));
        }
    }

    [Test]
    public async Task SendAsync_WhenCalled_ShouldConnectAndSend()
    {
        var options = new SmtpEmailOptions
        {
            Host = "localhost",
            Port = 25,
            UseSsl = false,
        };

        var client = new Mock<ISmtpClientAdapter>();
        var factory = new Mock<ISmtpClientAdapterFactory>();
        factory.Setup(x => x.Create()).Returns(client.Object);

        var sender = new SmtpEmailSender(options, factory.Object);

        var message = new EmailMessage
        {
            Subject = "Test",
        };

        await sender.SendAsync(message);

        client.Verify(x => x.ConnectAsync("localhost", 25, false, It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(x => x.SendAsync(It.Is<MimeMessage>(m => m.Subject == "Test"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendAsync_WhenUsernameAndPasswordExist_ShouldAuthenticate()
    {
        var options = new SmtpEmailOptions
        {
            Host = "localhost",
            Port = 25,
            UseSsl = true,
            Username = "user",
            Password = "pass",
        };

        var client = new Mock<ISmtpClientAdapter>();
        var factory = new Mock<ISmtpClientAdapterFactory>();
        factory.Setup(x => x.Create()).Returns(client.Object);

        var sender = new SmtpEmailSender(options, factory.Object);

        await sender.SendAsync(new EmailMessage { Subject = "Test" });

        client.Verify(x => x.AuthenticateAsync("user", "pass", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendAsync_WhenOnlyUsernameExists_ShouldNotAuthenticate()
    {
        var options = new SmtpEmailOptions
        {
            Host = "localhost",
            Port = 25,
            UseSsl = true,
            Username = "user",
            Password = null,
        };

        var client = new Mock<ISmtpClientAdapter>();
        var factory = new Mock<ISmtpClientAdapterFactory>();
        factory.Setup(x => x.Create()).Returns(client.Object);

        var sender = new SmtpEmailSender(options, factory.Object);

        await sender.SendAsync(new EmailMessage { Subject = "Test" });

        client.Verify(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
