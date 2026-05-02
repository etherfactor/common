using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace EtherGizmos.Common;

public static class NotificationsOpenTelemetryExtensions
{
    extension(TracerProviderBuilder @this)
    {
        public TracerProviderBuilder AddNotificationsInstrumentation()
        {
            return @this.AddSource(ActivitySources.Notifications.Name);
        }
    }

    extension(MeterProviderBuilder @this)
    {
        public MeterProviderBuilder AddNotificationsInstrumentation()
        {
            return @this; //No-op for now; no custom metrics
        }
    }
}
