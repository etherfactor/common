using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EtherGizmos.Common.Services;

internal class NotificationSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IEnumerable<IEventExtractor> _eventExtractors;

    public NotificationSaveChangesInterceptor(
        IEnumerable<IEventExtractor> eventExtractors)
    {
        _eventExtractors = eventExtractors;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        var events = ExtractAsync(eventData).GetAwaiter().GetResult();

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var events = await ExtractAsync(eventData, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task<IEnumerable<IDomainEvent>> ExtractAsync(
        DbContextEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var events = new List<IDomainEvent>();
        foreach (var entry in eventData.Context!.ChangeTracker.Entries())
        {
            foreach (var eventExtractor in _eventExtractors)
            {
                if (eventExtractor.CanHandle(entry))
                {
                    var toAdd = await eventExtractor.ExtractAsync(entry, cancellationToken);
                    events.AddRange(toAdd);
                }
            }
        }

        return events;
    }
}
