using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace EtherGizmos.Common;

public static class UnitOfWorkOpenTelemetryExtensions
{
    extension(TracerProviderBuilder @this)
    {
        public TracerProviderBuilder AddUnitOfWorkInstrumentation()
        {
            return @this.AddSource(ActivitySources.UnitOfWork.Name);
        }
    }

    extension(MeterProviderBuilder @this)
    {
        public MeterProviderBuilder AddUnitOfWorkInstrumentation()
        {
            return @this; //No-op for now; no custom metrics
        }
    }
}
