using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.RegularExpressions;

namespace EtherGizmos.Common.Services;

internal class NotificationCatalogProviderTests
{
    [Test]
    public void GetCatalog_WhenCalled_ShouldReturnCatalog()
    {
        //Arrange
        var optionsMock = new Mock<IOptions<NotificationEventOptions>>();

        var immediateSchema = JsonSchemaExporter
            .GetJsonSchemaAsNode(JsonSerializerOptions.Web, typeof(ImmediateScheduleConfig), new()
            {
                TreatNullObliviousAsNonNullable = true,
            })
            .ToJsonString(JsonSerializerOptions.Web);
        var immediateSchedule = new RegisteredNotificationSchedule(
            NotificationSchedules.Immediate.Id,
            "Immediate",
            typeof(ImmediateScheduleConfig), immediateSchema);

        var digestSchema = JsonSchemaExporter
            .GetJsonSchemaAsNode(JsonSerializerOptions.Web, typeof(DigestScheduleConfig), new()
            {
                TreatNullObliviousAsNonNullable = true,
            })
            .ToJsonString(JsonSerializerOptions.Web);
        var digestSchedule = new RegisteredNotificationSchedule(
            NotificationSchedules.Digest.Id,
            "Digest",
            typeof(DigestScheduleConfig), digestSchema);

        var testSchema = JsonSchemaExporter
            .GetJsonSchemaAsNode(JsonSerializerOptions.Web, typeof(TestSenderConfig), new()
            {
                TreatNullObliviousAsNonNullable = true,
            })
            .ToJsonString(JsonSerializerOptions.Web);
        var testChannel = new RegisteredNotificationChannel(
            "test",
            "Test Delivery",
            typeof(ImmediateScheduleConfig), testSchema);

        optionsMock.Setup(@interface =>
            @interface.Value)
            .Returns(new NotificationEventOptions()
            {
                Metadata =
                {
                    ["test.domain.event"] = new("test.domain.event", "Test Domain Event", typeof(TestEventConfig), "{}", [
                        new(immediateSchedule, testChannel),
                        new(digestSchedule, testChannel),
                    ]),
                },
            });

        var provider = new NotificationCatalogProvider(optionsMock.Object);

        //Act
        var catalog = provider.GetCatalog();
        var serialized = JsonSerializer.Serialize(catalog, JsonSerializerOptions.Web);
        serialized = new Regex("\"lastSeenAt\":\"[^\"]+\"").Replace(serialized, "\"lastSeenAt\":\"2000-01-01T00:00:00Z\"");

        //Assert
        Assert.That(serialized, Is.EqualTo("{\"events\":[{\"id\":\"test.domain.event\",\"name\":\"Test Domain Event\",\"isAvailable\":true,\"lastSeenAt\":\"2000-01-01T00:00:00Z\",\"configSchema\":{},\"supports\":[{\"eventId\":\"test.domain.event\",\"event\":null,\"channelId\":\"test\",\"channel\":null,\"scheduleId\":\"digest\",\"schedule\":null},{\"eventId\":\"test.domain.event\",\"event\":null,\"channelId\":\"test\",\"channel\":null,\"scheduleId\":\"immediate\",\"schedule\":null}]}],\"channels\":[{\"id\":\"test\",\"name\":\"Test Delivery\",\"isAvailable\":true,\"lastSeenAt\":\"2000-01-01T00:00:00Z\",\"configSchema\":{\"type\":\"object\"}}],\"schedules\":[{\"id\":\"digest\",\"name\":\"Digest\",\"isAvailable\":true,\"lastSeenAt\":\"2000-01-01T00:00:00Z\",\"configSchema\":{\"type\":\"object\",\"properties\":{\"cronExpression\":{\"type\":\"string\"}}}},{\"id\":\"immediate\",\"name\":\"Immediate\",\"isAvailable\":true,\"lastSeenAt\":\"2000-01-01T00:00:00Z\",\"configSchema\":{\"type\":\"object\"}}]}"));
    }

    private class TestSenderConfig;

    private class TestEventConfig;
}
