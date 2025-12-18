using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EtherGizmos.Common.Services;

internal class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IOptions<UnitOfWorkOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    public UnitOfWorkFactory(
        IOptions<UnitOfWorkOptions> options,
        IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public IUnitOfWork Create()
    {
        var scope = _serviceProvider.CreateScope();
        return new UnitOfWork(_options, scope);
    }

    public IUnitOfWork Create(
        IServiceProvider provider)
    {
        return new UnitOfWork(_options, provider);
    }

    public IUnitOfWork Create(
        bool useRequestScope)
    {
        if (useRequestScope)
        {
            var accessor = _serviceProvider.GetService<IHttpContextAccessor>()
                ?? throw new InvalidOperationException($"{nameof(Create)} can only be called with {nameof(useRequestScope)} of true in an ASP.NET Core application.");

            var context = accessor.HttpContext
                ?? throw new InvalidOperationException($"There is no active request to which to bind the unit of work.");

            return new UnitOfWork(_options, context.RequestServices);
        }

        return Create();
    }
}
