namespace EtherGizmos.Common.Abstractions;

public enum UnitOfWorkAmbientMode
{
    /// <summary>
    /// Always create a new UoW and set it ambient.
    /// </summary>
    CreateAmbient,

    /// <summary>
    /// If an ambient UoW exists, return it. Otherwise, create a new one and set it ambient.
    /// </summary>
    JoinOrCreateAmbient,

    /// <summary>
    /// Require that an ambient UoW exists; throw if not.
    /// </summary>
    RequireAmbient,

    /// <summary>
    /// Ignore ambient entirely; always create a new UoW and do NOT set ambient.
    /// </summary>
    SuppressAmbient,
}
