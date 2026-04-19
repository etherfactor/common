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
        => Create(new());

    public IUnitOfWork Create(UnitOfWorkCreateOptions options)
    {
        var accessor = _serviceProvider.GetRequiredService<IUnitOfWorkAccessor>();

        switch (options.AmbientMode)
        {
            case UnitOfWorkAmbientMode.JoinAmbientOrCreate:
            case UnitOfWorkAmbientMode.RequireAmbient:
                var ambient = accessor.Current;
                if (options.AmbientMode == UnitOfWorkAmbientMode.RequireAmbient && ambient is null)
                    throw new InvalidOperationException("An ambient unit of work scope was required, but no ambient scope is available.");

                ambient ??= Create(options with { AmbientMode = UnitOfWorkAmbientMode.CreateNewAndSetAmbient });
                return ambient;
        }

        UnitOfWork uow;
        switch (options.SccopeMode)
        {
            case UnitOfWorkScopeMode.NewScope:
                var scope = _serviceProvider.CreateScope();
                uow = new UnitOfWork(_options, scope);
                break;

            case UnitOfWorkScopeMode.ProvidedScope:
                ArgumentNullException.ThrowIfNull(options.ScopeProvider);
                uow = new UnitOfWork(_options, options.ScopeProvider);
                break;

            case UnitOfWorkScopeMode.RequestScope:
                var httpAccessor = _serviceProvider.GetService<IHttpContextAccessor>()
                    ?? throw new InvalidOperationException($"{nameof(Create)} can only use the request scope in an ASP.NET Core application.");

                var context = httpAccessor.HttpContext
                    ?? throw new InvalidOperationException($"There is no active request to which to bind the unit of work.");

                uow = new UnitOfWork(_options, context.RequestServices);
                break;

            default:
                throw new InvalidOperationException("Invalid options object.");
        }

        var disposable = accessor.Enter(uow);
        uow.AmbientDisposable = disposable;

        return uow;
    }
}
