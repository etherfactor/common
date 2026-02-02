using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EtherGizmos.Common.Services;

internal class NotificationSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IUnitOfWorkAccessor _uowAccessor;
    private readonly IEnumerable<IEventExtractor> _eventExtractors;

    public NotificationSaveChangesInterceptor(
        IUnitOfWorkFactory uowFactory,
        IUnitOfWorkAccessor uowAccessor,
        IEnumerable<IEventExtractor> eventExtractors)
    {
        _uowFactory = uowFactory;
        _uowAccessor = uowAccessor;
        _eventExtractors = eventExtractors;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        var owned = false;
        var uow = _uowAccessor.Current;

        try
        {
            if (uow is null)
            {
                uow = _uowFactory.Create();
                owned = true;
            }

            var events = ExtractAsync(eventData, uow).GetAwaiter().GetResult();

            return base.SavingChanges(eventData, result);
        }
        finally
        {
            if (owned && uow is not null)
            {
                uow.SaveChanges();
                uow.Dispose();
            }
        }
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var owned = false;
        var uow = _uowAccessor.Current;

        try
        {
            if (uow is null)
            {
                uow = _uowFactory.Create();
                owned = true;
            }

            var events = await ExtractAsync(eventData, uow, cancellationToken);

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
        finally
        {
            if (owned && uow is not null)
            {
                uow.SaveChanges();
                uow.Dispose();
            }
        }
    }

    private async Task<IEnumerable<IDomainEvent>> ExtractAsync(
        DbContextEventData eventData,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        var events = new List<IDomainEvent>();
        foreach (var entry in eventData.Context!.ChangeTracker.Entries())
        {
            foreach (var eventExtractor in _eventExtractors)
            {
                if (eventExtractor.CanHandle(entry))
                {
                    var toAdd = await eventExtractor.ExtractAsync(entry, unitOfWork, cancellationToken);
                    events.AddRange(toAdd);
                }
            }
        }

        return events;
    }
}
