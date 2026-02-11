namespace EtherGizmos.Common.Abstractions;

internal class DomainEventEmitter : IDomainEventEmitter
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IUnitOfWorkAccessor _uowAccessor;

    public DomainEventEmitter(
        IUnitOfWorkFactory uowFactory,
        IUnitOfWorkAccessor uowAccessor)
    {
        _uowFactory = uowFactory;
        _uowAccessor = uowAccessor;
    }

    public async Task EmitAsync(
        IDomainEvent @event,
        CancellationToken cancellationToken = default)
    {
        var managed = false;
        var uow = _uowAccessor.Current;
        if (uow is null)
        {
            managed = true;
            uow = _uowFactory.Create();
        }

        try
        {


            if (managed)
                await uow.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (managed)
                uow.Dispose();
        }
    }
}
