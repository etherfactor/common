using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class UnfilteredUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IUnitOfWorkFactory _inner;

    public UnfilteredUnitOfWorkFactory(
        IUnitOfWorkFactory inner)
    {
        _inner = inner;
    }

    public IUnitOfWork Create()
    {
        var uow = _inner.Create();
        var filter = uow.Services.GetRequiredService<IFilterContext>();
        filter.Disabled = true;

        return uow;
    }

    public IUnitOfWork Create(bool useRequestScope)
    {
        var uow = _inner.Create(useRequestScope);
        var filter = uow.Services.GetRequiredService<IFilterContext>();
        filter.Disabled = true;

        return uow;
    }

    public IUnitOfWork Create(IServiceProvider provider)
    {
        var uow = _inner.Create(provider);
        var filter = uow.Services.GetRequiredService<IFilterContext>();
        filter.Disabled = true;

        return uow;
    }
}
