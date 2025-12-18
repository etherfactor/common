namespace EtherGizmos.Common.Abstractions;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();

    IUnitOfWork Create(bool useRequestScope);

    IUnitOfWork Create(IServiceProvider provider);
}
