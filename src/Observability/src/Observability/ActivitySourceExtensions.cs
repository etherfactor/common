using System.Diagnostics;

namespace EtherGizmos.Common;

public static class ActivitySourceExtensions
{
    extension(ActivitySource @this)
    {
        public Activity? StartActivityFromCarrier(
            string name,
            ActivityKind kind,
            IReadOnlyDictionary<string, string> headers)
        {
            var parentContext = ActivityContextPropagator.UnpackContext(headers);

            var activity = parentContext != default
                ? @this.StartActivity(name, kind, parentContext)
                : @this.StartActivity(name, kind);

            ActivityContextPropagator.ApplyBaggage(headers, activity);

            return activity;
        }
    }
}
