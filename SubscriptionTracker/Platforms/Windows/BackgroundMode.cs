using System.Diagnostics;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using SubscriptionTracker.Services;

namespace SubscriptionTracker.WinUI;

/// <summary>
/// Windows background mode.
/// When the user signs in, Windows starts "SubscriptionTracker.exe --background" (registry Run key).
/// That process has no window: every few minutes it checks the subscriptions and shows
/// due payment reminders, so they arrive on time even when the app window is closed.
/// </summary>
public static class BackgroundMode
{
    public const string Argument = "--background";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "SubscriptionTracker";
    private const string MutexName = @"Local\SubscriptionTracker.Background";
    private const string StopEventName = @"Local\SubscriptionTracker.BackgroundStop";
    private const string WindowMutexName = @"Local\SubscriptionTracker.Window";
    private static Mutex? _windowMutex;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    public static bool IsBackgroundLaunch =>
        Environment.GetCommandLineArgs().Any(a => a.Equals(Argument, StringComparison.OrdinalIgnoreCase));

    /// <summary>Adds or removes the app from Windows startup.</summary>
    public static void SetStartWithWindows(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (enabled && Environment.ProcessPath is { } exe)
                key.SetValue(RunValueName, $"\"{exe}\" {Argument}");
            else
                key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Startup registration failed: {ex}");
        }
    }

    public static bool IsRunning()
    {
        try
        {
            using var _ = Mutex.OpenExisting(MutexName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Starts the background process now (if it isn't running yet).</summary>
    public static void Start()
    {
        if (IsRunning() || Environment.ProcessPath is not { } exe) return;
        try
        {
            Process.Start(new ProcessStartInfo(exe, Argument) { UseShellExecute = false });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Background start failed: {ex}");
        }
    }

    /// <summary>Asks the background process to quit.</summary>
    public static void Stop()
    {
        try
        {
            using var stop = EventWaitHandle.OpenExisting(StopEventName);
            stop.Set();
        }
        catch
        {
            // not running
        }
    }

    /// <summary>Applies the "Start with Windows" setting: registry entry + running process.</summary>
    public static void Apply(bool enabled)
    {
        SetStartWithWindows(enabled);
        if (enabled) Start(); else Stop();
    }

    /// <summary>Called by the app window: lets the background process know that the window is open.</summary>
    public static void MarkWindowOpen() => _windowMutex ??= new Mutex(false, WindowMutexName);

    private static bool IsWindowOpen()
    {
        try
        {
            using var _ = Mutex.OpenExisting(WindowMutexName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Installs a new version silently — only while the app window is closed,
    /// so the user is never interrupted. Returns true when the installer was started.
    /// </summary>
    private static async Task<bool> TryAutoUpdateAsync(SettingsService settings, UpdateService updates)
    {
        if (!settings.AutoUpdate || IsWindowOpen()) return false;
        try
        {
            var info = await updates.CheckAsync();
            if (info is null) return false;

            var installer = await updates.DownloadAsync(info);
            if (IsWindowOpen()) return false;   // the window was opened meanwhile — it will offer the update
            UpdateService.StartInstaller(installer, restartInBackground: true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Auto-update failed: {ex}");
            return false;
        }
    }

    /// <summary>Main loop of the background process. Returns when it should exit.</summary>
    public static async Task RunAsync()
    {
        using var mutex = new Mutex(true, MutexName, out var isFirst);
        if (!isFirst) return;   // another background process is already running

        using var stop = new EventWaitHandle(false, EventResetMode.AutoReset, StopEventName);

        // Clicking a notification opens the app window
        ToastNotificationManagerCompat.OnActivated += _ => OpenAppWindow();

        var settings = new SettingsService();
        var reminders = new ReminderService(settings);
        var db = new DatabaseService(new LogoService());
        var updates = new UpdateService(settings);

        while (settings.StartWithWindows)
        {
            try
            {
                await reminders.RescheduleAsync(await db.GetAllAsync());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Background check failed: {ex}");
            }

            // The installer closes this process and starts the new version
            if (await TryAutoUpdateAsync(settings, updates)) break;

            var stopRequested = await Task.Run(() => stop.WaitOne(CheckInterval));
            if (stopRequested) break;
        }
    }

    private static void OpenAppWindow()
    {
        if (Environment.ProcessPath is { } exe)
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false });
    }
}
