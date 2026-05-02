using Microsoft.EntityFrameworkCore;

namespace EtherGizmos.Common.Abstractions;

public interface IDbContextBuilder<TOptions>
{
    void ConfigureContext(
        DbContextOptionsBuilder builder,
        TOptions options,
        Action<IRelationalEfOptions>? configureOptions);
}
