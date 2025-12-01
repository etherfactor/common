namespace EtherGizmos.Common.Abstractions;

public interface IAbstractResolver<TOptions>
    where TOptions : class, new()
{
    IServiceProvider ServiceProvider { get; }

    string SectionName { get; }

    Dictionary<string, TOptions> Options { get; }
}
