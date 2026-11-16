using System.Windows;

namespace BROZILLA;

public partial class App : System.Windows.Application
{
    private const string MutexName = "Global\\BROZILLA_SingleInstance_Mutex";
    private const string WakeEventName = "Global\\BROZILLA_WakeUp_Event";

    private Mutex? _mutex;
    private EventWaitHandle? _wakeEvent;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Signal existing instance to bring window to front
            try
            {
                using var ev = EventWaitHandle.OpenExisting(WakeEventName);
                ev.Set();
            }
            catch { }

            Shutdown();
            return;
        }

        _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WakeEventName);

        bool startMinimized = e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase));
        var mainWindow = new MainWindow(startMinimized);
        MainWindow = mainWindow;

        if (!startMinimized)
        {
            mainWindow.Show();
        }

        // Listen for second-instance wake signals
        ThreadPool.RegisterWaitForSingleObject(_wakeEvent, (_, _) =>
        {
            Dispatcher.BeginInvoke(() => mainWindow.RestoreFromTray());
        }, null, -1, false);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _wakeEvent?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
