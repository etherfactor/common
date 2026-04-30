using System.Collections.Immutable;
using System.Diagnostics;

namespace EtherGizmos.Common.Abstractions;

public record ReceivedMessage
{
    public required string MessageId { get; init; }

    public required string Type { get; init; }

    public required string Body { get; init; }

    public required ImmutableDictionary<string, string> Headers { get; init; }

    public required string LogicalSourceName { get; init; }

    public required string SubscriptionName { get; init; }

    public required IMessageActions Actions { get; init; }

    public string? ConsumerName { get; init; }
}

public static class ReceivedMessageExtensions
{
    extension(ReceivedMessage @this)
    {
        /// <summary>
        /// All headers, including metadata headers.
        /// </summary>
        public ImmutableDictionary<string, string> AllHeaders => @this.Headers
            .SetItem("$type", @this.Type)
            .SetItem("$logical", @this.LogicalSourceName);

        /// <summary>
        /// Adds new headers to the message. If the header already exists, its value will be replaced.
        /// </summary>
        /// <param name="headers">The headers to add.</param>
        /// <returns>A new message instance, with the new headers.</returns>
        public ReceivedMessage AddHeaders(
            IReadOnlyDictionary<string, string> headers)
        {
            var final = @this.Headers;
            foreach (var header in headers)
            {
                final.SetItem(header.Key, header.Value);
            }

            return @this with { Headers = final };
        }

        /// <summary>
        /// Adds headers from an <see cref="Activity"/> to the message. If the header already exists, its value will be replaced.
        /// </summary>
        /// <param name="activity">The activity from which to add headers.</param>
        /// <returns>A new message instance, with the new headers.</returns>
        public ReceivedMessage AddActivityHeaders(
            Activity? activity)
        {
            var context = ActivityContextPropagator.Pack(activity);
            return @this.AddHeaders(context);
        }
    }
}
