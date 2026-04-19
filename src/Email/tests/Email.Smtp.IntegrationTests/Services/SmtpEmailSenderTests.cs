using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common.Services;

internal class SmtpEmailSenderTests
{
    [SetUp]
    public async Task SetUp()
    {
        await DeleteAllMessagesAsync();
    }

    [Test]
    public async Task SendAsync_WithTextBody_ShouldSendToSmtpServer()
    {
        var sender = new SmtpEmailSender(new SmtpEmailOptions()
        {
            Host = Setup.MailHogHost,
            Port = Setup.MailHogPort,
            UseSsl = false,
            Username = null,
            Password = null,
        }, new MailKitSmtpClientAdapterFactory());

        var subject = $"smtp-it-{Guid.NewGuid():N}";
        var body = $"body-{Guid.NewGuid():N}";
        var recipient = "recipient@test.local";

        var message = new EmailMessage
        {
            From = new EmailAddress("Sender", "sender@test.local"),
            Subject = subject,
            TextBody = body,
            To =
            [
                new EmailAddress("Recipient", recipient)
            ]
        };

        await sender.SendAsync(message);

        var rawJson = await WaitForMessageAsync(subject);

        Assert.That(rawJson, Does.Contain(subject));
        Assert.That(rawJson, Does.Contain(body));
        Assert.That(rawJson, Does.Contain(recipient));
        Assert.That(rawJson, Does.Contain("sender@test.local"));
    }

    [Test]
    public async Task SendAsync_WithHtmlBody_ShouldSendToSmtpServer()
    {
        var sender = new SmtpEmailSender(new SmtpEmailOptions()
        {
            Host = Setup.MailHogHost,
            Port = Setup.MailHogPort,
            UseSsl = false,
            Username = null,
            Password = null,
        }, new MailKitSmtpClientAdapterFactory());

        var subject = $"smtp-html-it-{Guid.NewGuid():N}";
        var html = $"<strong>html-{Guid.NewGuid():N}</strong>";

        var message = new EmailMessage
        {
            From = new EmailAddress("Sender", "sender@test.local"),
            Subject = subject,
            HtmlBody = html,
            To =
            [
                new EmailAddress("Recipient", "recipient@test.local")
            ]
        };

        await sender.SendAsync(message);

        var rawJson = await WaitForMessageAsync(subject);

        Assert.That(rawJson, Does.Contain(subject));
        Assert.That(rawJson, Does.Contain("recipient@test.local"));
        Assert.That(rawJson, Does.Contain("sender@test.local"));
        Assert.That(rawJson, Does.Contain("html-"));
    }

    private async Task<string> WaitForMessageAsync(string subject, int attempts = 20, int delayMs = 250)
    {
        for (var i = 0; i < attempts; i++)
        {
            var json = await Setup.MailHogClient.GetStringAsync("/api/v2/messages");

            if (json.Contains(subject, StringComparison.Ordinal))
            {
                return json;
            }

            await Task.Delay(delayMs);
        }

        Assert.Fail($"Did not find message with subject '{subject}' in MailHog.");
        return null!;
    }

    private async Task DeleteAllMessagesAsync()
    {
        try
        {
            using var response = await Setup.MailHogClient.DeleteAsync("/api/v1/messages");
            response.EnsureSuccessStatusCode();
        }
        catch { }
    }
}
