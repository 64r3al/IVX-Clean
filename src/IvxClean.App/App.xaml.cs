using System.Threading;
using System.Windows;

namespace IvxClean.App;

/// <summary>
/// Application entry point. Enforces single-instance behaviour via a named mutex;
/// a second launch activates the existing window instead of starting again.
/// </summary>
public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Global\IVXClean.SingleInstance.9E2C1A77";
    private static Mutex? _singleInstanceMutex;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
        if (!createdNew)
        {
            // Another instance already owns the mutex. Full cross-process activation
            // is wired in the UI phase; for now we simply decline to start a second copy.
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;
        base.OnExit(e);
    }
}
