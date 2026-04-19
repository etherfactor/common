using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EtherGizmos.Common.Services;

public sealed class RelationalEfOptions<TBuilder, TExtension> : IRelationalEfOptions
    where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
    where TExtension : RelationalOptionsExtension, new()
{
    private readonly TBuilder _builder;

    public RelationalEfOptions(
        TBuilder builder)
    {
        _builder = builder;
    }

    public IRelationalEfOptions UseQuerySplittingBehavior(
        QuerySplittingBehavior querySplittingBehavior)
    {
        _builder.UseQuerySplittingBehavior(querySplittingBehavior);
        return this;
    }

    public IRelationalEfOptions CommandTimeout(
        int? commandTimeout)
    {
        _builder.CommandTimeout(commandTimeout);
        return this;
    }

    public IRelationalEfOptions If<TOptions>(
        Action<TOptions> configureOptions)
    {
        if (_builder is TOptions options)
            configureOptions(options);

        return this;
    }
}
