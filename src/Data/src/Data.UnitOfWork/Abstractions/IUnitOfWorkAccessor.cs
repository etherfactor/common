namespace EtherGizmos.Common.Abstractions;

public interface IUnitOfWorkAccessor
{
    IUnitOfWork? Current { get; }

    IDisposable Enter(IUnitOfWork uow);
}
