namespace EtherGizmos.Common.Abstractions;

public interface IModularConfigurationResolver<TOptions>
    where TOptions : class, new()
{
    IServiceProvider ServiceProvider { get; }

    string SectionName { get; }

    Dictionary<string, TOptions> Options { get; }
}
