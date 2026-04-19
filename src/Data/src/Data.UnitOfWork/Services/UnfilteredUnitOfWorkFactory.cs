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

    public IUnitOfWork Create(UnitOfWorkCreateOptions options)
    {
        var uow = _inner.Create(options);
        var filter = uow.Services.GetRequiredService<IFilterContext>();
        filter.Disabled = true;

        return uow;
    }
}
