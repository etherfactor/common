using Microsoft.EntityFrameworkCore;

namespace EtherGizmos.Common.Abstractions;

public interface IRelationalEfOptions
{
    IRelationalEfOptions CommandTimeout(int? commandTimeout);

    IRelationalEfOptions If<TOptions>(Action<TOptions> configureOptions);

    IRelationalEfOptions UseQuerySplittingBehavior(QuerySplittingBehavior querySplittingBehavior);
}
