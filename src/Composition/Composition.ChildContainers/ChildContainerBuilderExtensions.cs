using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EtherGizmos.Common;

/// <summary>
/// Provides extension methods for <see cref="IChildContainerBuilder"/>.
/// </summary>
public static class ChildContainerBuilderExtensions
{
    extension(IChildContainerBuilder @this)
    {
        /// <summary>
        /// Imports the parent container's logging into the child container, so references to <see cref="ILoggerFactory"/> and
        /// <see cref="ILogger{TCategoryName}"/> resolve to the parent's loggers.
        /// </summary>
        /// <returns>Itself.</returns>
        public IChildContainerBuilder ImportLogging()
        {
            @this.ImportSingleton<ILoggerFactory>();
            @this.ChildServices.AddSingleton(typeof(ILogger<>), typeof(LoggerForward<>));

            return @this;
        }
    }
}
