using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using System.Text.Json.Schema;

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
            NotificationSchedules.Immediate.Key,
            "Immediate",
            typeof(ImmediateScheduleConfig), immediateSchema);

        var digestSchema = JsonSchemaExporter
            .GetJsonSchemaAsNode(JsonSerializerOptions.Web, typeof(DigestScheduleConfig), new()
            {
                TreatNullObliviousAsNonNullable = true,
            })
            .ToJsonString(JsonSerializerOptions.Web);
        var digestSchedule = new RegisteredNotificationSchedule(
            NotificationSchedules.Digest.Key,
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
                    ["test.domain.event"] = new("test.domain.event", "Test Domain Event", [
                        new(immediateSchedule, testChannel),
                        new(digestSchedule, testChannel),
                    ]),
                },
            });

        var provider = new NotificationCatalogProvider(optionsMock.Object);

        //Act
        var catalog = provider.GetCatalog();
        var serialized = JsonSerializer.Serialize(catalog, JsonSerializerOptions.Web);

        //Assert
        Assert.That(serialized, Is.EqualTo("{\"events\":[{\"eventKey\":\"test.domain.event\",\"displayName\":\"Test Domain Event\",\"supports\":[{\"channelKey\":\"test\",\"scheduleKey\":\"digest\"},{\"channelKey\":\"test\",\"scheduleKey\":\"immediate\"}]}],\"channels\":[{\"channelKey\":\"test\",\"displayName\":\"Test Delivery\",\"configSchema\":{\"type\":\"object\"}}],\"schedules\":[{\"scheduleKey\":\"digest\",\"displayName\":\"Digest\",\"configSchema\":{\"type\":\"object\",\"properties\":{\"cronExpression\":{\"type\":\"string\"}}}},{\"scheduleKey\":\"immediate\",\"displayName\":\"Immediate\",\"configSchema\":{\"type\":\"object\"}}]}"));
    }

    private class TestSenderConfig;
}
