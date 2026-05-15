using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Runtime.CompilerServices;

namespace EtherGizmos.Common.Services;

internal class NotificationSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IUnitOfWorkAccessor _uowAccessor;
    private readonly IEnumerable<IDomainEventExtractor> _eventExtractors;
    private readonly IDomainEventEmitter _eventEmitter;

    public NotificationSaveChangesInterceptor(
        IUnitOfWorkFactory uowFactory,
        IUnitOfWorkAccessor uowAccessor,
        IEnumerable<IDomainEventExtractor> eventExtractors,
        IDomainEventEmitter eventEmitter)
    {
        _uowFactory = uowFactory;
        _uowAccessor = uowAccessor;
        _eventExtractors = eventExtractors;
        _eventEmitter = eventEmitter;
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

            var events = ExtractAsync(eventData, uow);

            foreach (var @event in events.ToBlockingEnumerable())
            {
                _eventEmitter.EmitAsync(@event.Event, @event.Audience).GetAwaiter().GetResult();
            }

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
        using var uow = _uowFactory.Create(new() { AmbientMode = UnitOfWorkAmbientMode.CreateNewAndSetAmbient });

        try
        {
            var events = ExtractAsync(eventData, uow, cancellationToken);

            await foreach (var @event in events)
            {
                await _eventEmitter.EmitAsync(@event.Event, @event.Audience, cancellationToken: cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
        finally
        {
            uow.SaveChanges();
        }
    }

    private async IAsyncEnumerable<DomainEventEmission> ExtractAsync(
        DbContextEventData eventData,
        IUnitOfWork unitOfWork,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var entry in eventData.Context!.ChangeTracker.Entries())
        {
            foreach (var eventExtractor in _eventExtractors)
            {
                if (eventExtractor.CanHandle(entry))
                {
                    var toAddEvents = eventExtractor.ExtractAsync(entry, unitOfWork, cancellationToken);
                    await foreach (var toAddEvent in toAddEvents)
                    {
                        yield return toAddEvent;
                    }
                }
            }
        }
    }
}
