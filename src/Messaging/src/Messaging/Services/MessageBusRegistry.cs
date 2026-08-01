using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace EtherGizmos.Common.Services;

internal sealed class MessageBusRegistry : IMessageBusRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, string> _buses = [];
    private readonly ConcurrentDictionary<string, byte> _listeners = [];
    private readonly ConcurrentDictionary<string, byte> _publishers = [];

    private readonly TaskCompletionSource _onReadySource = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public Task OnReady => _onReadySource.Task;

    public MessageBusRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void MarkReady()
        => _onReadySource.TrySetResult();

    public bool TryGetBusId(
        string logicalName,
        [NotNullWhen(true)] out string? busId)
        => _buses.TryGetValue(logicalName, out busId);

    public bool TryGetBus(
        string busId,
        [NotNullWhen(true)] out IMessageBus? bus)
    {
        var key = new BusKey(busId);
        bus = _serviceProvider.GetKeyedService<IMessageBus>(key);
        return bus is not null;
    }

    public void RegisterListener(string busId, string logicalName)
    {
        RegisterBusMapping(busId, logicalName);

        if (!_listeners.TryAdd(logicalName, 0))
        {
            RemoveBusMappingIfUnused(logicalName);
            throw new InvalidOperationException(
                $"The listener '{logicalName}' is already registered.");
        }
    }

    public void RegisterPublisher(string busId, string logicalName)
    {
        RegisterBusMapping(busId, logicalName);

        if (!_publishers.TryAdd(logicalName, 0))
        {
            RemoveBusMappingIfUnused(logicalName);
            throw new InvalidOperationException(
                $"The publisher '{logicalName}' is already registered.");
        }
    }

    public void UnregisterListener(string logicalName)
    {
        _listeners.TryRemove(logicalName, out _);
        RemoveBusMappingIfUnused(logicalName);
    }

    public void UnregisterPublisher(string logicalName)
    {
        _publishers.TryRemove(logicalName, out _);
        RemoveBusMappingIfUnused(logicalName);
    }

    private void RegisterBusMapping(string busId, string logicalName)
    {
        while (true)
        {
            if (_buses.TryGetValue(logicalName, out var currentBusId))
            {
                if (currentBusId != busId)
                {
                    throw new InvalidOperationException(
                        $"The logical name '{logicalName}' cannot be registered to bus '{busId}' " +
                        $"because it is already registered to bus '{currentBusId}'.");
                }

                return;
            }

            if (_buses.TryAdd(logicalName, busId))
                return;
        }
    }

    private void RemoveBusMappingIfUnused(string logicalName)
    {
        if (!_listeners.ContainsKey(logicalName) &&
            !_publishers.ContainsKey(logicalName))
        {
            _buses.TryRemove(logicalName, out _);
        }
    }
}
