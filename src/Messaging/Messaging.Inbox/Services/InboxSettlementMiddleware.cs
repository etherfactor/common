using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class InboxSettlementMiddleware : IMessageMiddleware
{
    private readonly IMessageSettlement _settlement;

    public InboxSettlementMiddleware(
        IMessageSettlement settlement)
    {
        _settlement = settlement;
    }

    public async Task InvokeAsync(
        ReceivedMessage message,
        Func<Task> next)
    {
        try
        {
            await next();

            if (!message.Actions.Invoked)
                await message.Actions.CompleteAsync();
        }
        catch (Exception)
        {
            if (!message.Actions.Invoked)
                await message.Actions.AbandonAsync();

            throw;
        }
        finally
        {
            await _settlement.FinalizeAsync();
        }
    }
}
