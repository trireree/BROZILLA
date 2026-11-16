using System.Diagnostics;
using System.IO;
using System.Net.Http;
using BROZILLA.Models;

namespace BROZILLA.Services;

/// <summary>
/// Manages the embedded goodbyedpi.exe process, legacy service cleanup,
/// Windows startup task registration, and connectivity verification.
/// </summary>
public sealed class GoodbyeDpiManager : IDisposable
{
    private const string TaskName = "BROZILLA_AutoStart";

    private readonly string _engineDir;
    private readonly string _exePath;
    private readonly object _lock = new();
    private Process? _process;

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _process is { HasExited: false };
            }
        }
    }

    public DateTime? StartedAtUtc { get; private set; }
    public DpiProfile? ActiveProfile { get; private set; }

    public event Action<bool>? StateChanged;

    public GoodbyeDpiManager()
    {
        _engineDir = Path.Combine(AppContext.BaseDirectory, "engine");
        _exePath = Path.Combine(_engineDir, "goodbyedpi.exe");
    }

    public (bool Success, string? ErrorMessage) Start(DpiProfile profile)
    {
        lock (_lock)
        {
            if (!File.Exists(_exePath))
            {
                return (false, "Motor dosyası bulunamadı: engine\\goodbyedpi.exe (Antivirüs silmiş olabilir).");
            }

            StopInternal();
            CleanupConflictingProcessesAndServices();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _exePath,
                    Arguments = profile.Arguments,
                    WorkingDirectory = _engineDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                proc.Exited += (_, _) =>
                {
                    lock (_lock)
                    {
                        if (_process == proc)
                        {
                            _process = null;
                            StartedAtUtc = null;
                            ActiveProfile = null;
                            StateChanged?.Invoke(false);
                        }
                    }
                };

                if (!proc.Start())
                {
                    return (false, "GoodbyeDPI başlatılamadı.");
                }

                // Brief check to catch immediate exit (e.g. WinDivert blocked)
                Thread.Sleep(180);
                if (proc.HasExited)
                {
                    return (false, $"GoodbyeDPI hemen kapandı (Kod: {proc.ExitCode}). Antivirüs WinDivert64.sys dosyasını engelliyor olabilir.");
                }

                _process = proc;
                StartedAtUtc = DateTime.UtcNow;
                ActiveProfile = profile;
                StateChanged?.Invoke(true);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Başlatma hatası: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            StopInternal();
            StateChanged?.Invoke(false);
        }
    }

    private void StopInternal()
    {
        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    _process.WaitForExit(2000);
                }
            }
            catch
            {
                // Ignore kill errors
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        StartedAtUtc = null;
        ActiveProfile = null;
    }

    private static void CleanupConflictingProcessesAndServices()
    {
        // Kill any stray goodbyedpi.exe instances
        foreach (var p in Process.GetProcessesByName("goodbyedpi"))
        {
            try
            {
                p.Kill(entireProcessTree: true);
                p.WaitForExit(1000);
            }
            catch { }
            finally { p.Dispose(); }
        }

        // Stop legacy GoodbyeDPI Windows service if installed
        RunHidden("sc.exe", "stop GoodbyeDPI");
    }

    public static async Task<(bool Ok, long LatencyMs, string Message)> TestConnectionAsync(CancellationToken ct = default)
    {
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(4)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("BROZILLA/1.0");

        var sw = Stopwatch.StartNew();
        try
        {
            using var resp = await client.GetAsync("https://discord.com", HttpCompletionOption.ResponseHeadersRead, ct);
            sw.Stop();
            return (true, sw.ElapsedMilliseconds, $"Erişim Açık ({sw.ElapsedMilliseconds} ms)");
        }
        catch
        {
            sw.Stop();
            return (false, sw.ElapsedMilliseconds, "Bağlantı Engelli / Zaman Aşımı");
        }
    }

    public static bool SetStartWithWindows(bool enable)
    {
        if (enable)
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe)) return false;
            var args = $"/Create /TN \"{TaskName}\" /TR \"\\\"{exe}\\\" --minimized\" /SC ONLOGON /RL HIGHEST /F";
            return RunHidden("schtasks.exe", args) == 0;
        }
        else
        {
            RunHidden("schtasks.exe", $"/Delete /TN \"{TaskName}\" /F");
            return true;
        }
    }

    private static int RunHidden(string fileName, string arguments)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            proc?.WaitForExit(2500);
            return proc?.ExitCode ?? -1;
        }
        catch
        {
            return -1;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
