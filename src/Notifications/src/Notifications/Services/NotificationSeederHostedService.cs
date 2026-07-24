using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EtherGizmos.Common.Services;

internal class NotificationSeederHostedService : IHostedService
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly INotificationCatalogProvider _catalogProvider;

    public NotificationSeederHostedService(
        IUnitOfWorkFactory uowFactory,
        INotificationCatalogProvider catalogProvider)
    {
        _uowFactory = uowFactory;
        _catalogProvider = catalogProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var uow = _uowFactory.Create();

        //This is a bit hacky, but EF Core doesn't like records
        var context = uow.Services.GetRequiredService<NotificationContext>();

        var catalog = _catalogProvider.GetCatalog();

        var channelRepo = uow.Repository<NotificationChannel>();
        var handleChannels = catalog.Channels.ToList();
        var currentChannels = await channelRepo.Data.ToListAsync(cancellationToken: cancellationToken);
        foreach (var channel in currentChannels)
        {
            var channelMeta = handleChannels.SingleOrDefault(e => e.Id == channel.Id);
            var updated = channel;
            if (channelMeta is not null)
            {
                handleChannels.Remove(channelMeta);
                updated = channel with
                {
                    Name = channelMeta.Name,
                    IsAvailable = true,
                    LastSeenAt = channelMeta.LastSeenAt,
                    ConfigSchema = channelMeta.ConfigSchema,
                };
            }
            else
            {
                updated = channel with
                {
                    IsAvailable = false,
                };
            }

            context.Entry(channel).CurrentValues.SetValues(updated);
        }

        foreach (var channel in handleChannels)
        {
            channelRepo.Add(channel);
        }

        var scheduleRepo = uow.Repository<NotificationSchedule>();
        var handleSchedules = catalog.Schedules.ToList();
        var currentSchedules = await scheduleRepo.Data.ToListAsync(cancellationToken: cancellationToken);
        foreach (var schedule in currentSchedules)
        {
            var scheduleMeta = handleSchedules.SingleOrDefault(e => e.Id == schedule.Id);
            var updated = schedule;
            if (scheduleMeta is not null)
            {
                handleSchedules.Remove(scheduleMeta);
                updated = schedule with
                {
                    Name = scheduleMeta.Name,
                    IsAvailable = true,
                    LastSeenAt = scheduleMeta.LastSeenAt,
                    ConfigSchema = scheduleMeta.ConfigSchema,
                };
            }
            else
            {
                updated = schedule with
                {
                    IsAvailable = false,
                };
            }

            context.Entry(schedule).CurrentValues.SetValues(updated);
        }

        foreach (var schedule in handleSchedules)
        {
            scheduleRepo.Add(schedule);
        }

        var eventRepo = uow.Repository<NotificationEvent>();
        var handleEvents = catalog.Events.ToList();
        var currentEvents = await eventRepo.Data.ToListAsync(cancellationToken: cancellationToken);
        foreach (var @event in currentEvents)
        {
            var eventMeta = handleEvents.SingleOrDefault(e => e.Id == @event.Id);
            var updated = @event;
            if (eventMeta is not null)
            {
                handleEvents.Remove(eventMeta);
                updated = @event with
                {
                    Name = eventMeta.Name,
                    IsAvailable = true,
                    LastSeenAt = eventMeta.LastSeenAt,
                    ConfigSchema = eventMeta.ConfigSchema,
                };
            }
            else
            {
                updated = @event with
                {
                    IsAvailable = false,
                };
            }

            context.Entry(@event).CurrentValues.SetValues(updated);
        }

        foreach (var @event in handleEvents)
        {
            eventRepo.Add(@event);
        }

        await uow.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
