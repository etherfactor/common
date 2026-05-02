namespace EtherGizmos.Common.Abstractions;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();

    IUnitOfWork Create(UnitOfWorkCreateOptions options);
}
