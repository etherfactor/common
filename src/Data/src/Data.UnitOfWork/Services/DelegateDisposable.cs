namespace EtherGizmos.Common.Services;

internal class DelegateDisposable : IDisposable
{
    private readonly Action _action;

    private bool _disposed;

    public DelegateDisposable(
        Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _action = action;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _action.Invoke();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
