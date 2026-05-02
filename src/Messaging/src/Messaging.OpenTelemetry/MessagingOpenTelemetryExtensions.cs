using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace EtherGizmos.Common;

public static class MessagingOpenTelemetryExtensions
{
    extension(TracerProviderBuilder @this)
    {
        public TracerProviderBuilder AddMessagingInstrumentation()
        {
            return @this.AddSource(ActivitySources.Messaging.Name);
        }
    }

    extension(MeterProviderBuilder @this)
    {
        public MeterProviderBuilder AddMessagingInstrumentation()
        {
            return @this; //No-op for now; no custom metrics
        }
    }
}
