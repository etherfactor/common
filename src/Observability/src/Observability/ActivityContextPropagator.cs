using System.Collections.Immutable;
using System.Diagnostics;

namespace EtherGizmos.Common;

public static class ActivityContextPropagator
{
    public static IReadOnlyDictionary<string, string> Pack(
        Activity? activity)
    {
        if (activity is null) return ImmutableDictionary<string, string>.Empty;

        //Extract the current activity context
        var carrier = new Dictionary<string, string>();
        if (activity is not null)
        {
            DistributedContextPropagator.Current.Inject(activity, carrier, (c, key, value) =>
            {
                var carrier = (Dictionary<string, string>)c!;
                carrier[key] = value;
            });
        }

        return carrier.AsReadOnly();
    }

    public static ActivityContext UnpackContext(
        IReadOnlyDictionary<string, string> carrier)
    {
        //Read the activity context from the message
        DistributedContextPropagator.Current.ExtractTraceIdAndState(carrier, (c, key, out value, out values) =>
        {
            values = null;
            var headers = (IReadOnlyDictionary<string, string>)c!;
            headers.TryGetValue(key, out value);
        }, out var traceId, out var traceState);

        //Attempt to extract the parent context
        var traceParent = carrier.TryGetValue("traceparent", out var tp) ? tp : null;
        ActivityContext parentContext = default;
        if (!string.IsNullOrWhiteSpace(traceParent))
        {
            ActivityContext.TryParse(traceParent, traceState, out parentContext);
        }

        return parentContext;
    }

    public static void ApplyBaggage(
        IReadOnlyDictionary<string, string> carrier,
        Activity? activity)
    {
        if (activity is null) return;

        //Extract the baggage
        var baggage = DistributedContextPropagator.Current.ExtractBaggage(carrier, (c, key, out value, out values) =>
        {
            values = null;
            var headers = (IReadOnlyDictionary<string, string>)c!;
            headers.TryGetValue(key, out value);
        })?.ToDictionary() ?? [];

        //Set the baggage on the new activity
        foreach (var key in baggage.Keys)
        {
            activity?.SetBaggage(key, baggage[key]);
        }
    }
}
