using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

public enum UnitOfWorkScopeMode
{
    /// <summary>
    /// Creates a new <see cref="IServiceScope"/> internally.
    /// </summary>
    NewScope,

    /// <summary>
    /// Uses the request scope from <see cref="IHttpContextAccessor"/>.
    /// </summary>
    RequestScope,

    /// <summary>
    /// Uses the <see cref="IServiceProvider"/> provided in the options.
    /// </summary>
    ProvidedScope,
}
