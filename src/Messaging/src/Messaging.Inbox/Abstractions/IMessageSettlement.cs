namespace EtherGizmos.Common.Abstractions;

public interface IMessageSettlement
{
    void Register(string consumerName, IMessageActions actions);

    void SetExpected(Func<int> expected);

    void Abandon(string consumerName);

    void Complete(string consumerName);

    void DeadLetter(string consumerName);

    Task FinalizeAsync(CancellationToken cancellationToken = default);
}
