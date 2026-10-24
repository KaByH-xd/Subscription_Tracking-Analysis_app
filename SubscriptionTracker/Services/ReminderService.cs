using SubscriptionTracker.Models;
#if WINDOWS
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
#else
using Plugin.LocalNotification;
#endif

namespace SubscriptionTracker.Services;

/// <summary>
/// System notifications about upcoming payments.
///
/// A reminder is sent EVERY DAY during the reminder window at 10:00:
/// with "3 days" the user gets "in 3 days", "in 2 days" and "tomorrow".
/// If the device was off at 10:00, today's reminder is shown as soon as possible.
///
/// Android: reminders are scheduled with Plugin.LocalNotification (they arrive even when the app is closed).
/// Windows: the app (or its background process started at sign-in) checks every few minutes
///          and shows due reminders as toast notifications.
/// Every reminder is shown only once — this is tracked with small marker files.
/// </summary>
public class ReminderService
{
    public const int ReminderHour = 10;
    private const int TestNotificationId = 999_999;
    private readonly SettingsService _settings;

    public ReminderService(SettingsService settings) => _settings = settings;

    /// <summary>One reminder: "Netflix payment in 2 days".</summary>
    public record Reminder(Subscription Subscription, int DaysBefore, DateTime NotifyAt)
    {
        public int NotificationId => 100_000 + Subscription.Id * 10 + DaysBefore;
        public string Key => $"{Subscription.Id}_{Subscription.NextPaymentDate:yyyyMMdd}_{DaysBefore}";
        public string Title => $"🔔 {Subscription.Name}: payment {When}";
        public string Body => $"{Subscription.AmountText} will be charged on {Subscription.NextPaymentDate:dddd, MMM d}.";
        private string When => DaysBefore == 1 ? "tomorrow" : $"in {DaysBefore} days";
    }

    /// <summary>All reminders for the current payments (e.g. 3, 2 and 1 day before each payment).</summary>
    public IEnumerable<Reminder> BuildPlan(IEnumerable<Subscription> subscriptions)
    {
        var window = _settings.RemindDays;
        foreach (var s in subscriptions.Where(s => s.IsActive && s.NextPaymentDate.Date >= DateTime.Today))
            for (var d = window; d >= 1; d--)
                yield return new Reminder(s, d, s.NextPaymentDate.Date.AddDays(-d).AddHours(ReminderHour));
    }

    /// <summary>Reminders that should be on screen now: today's ones after 10:00 (also when the PC was off at 10:00).</summary>
    private IEnumerable<Reminder> DueNow(IEnumerable<Subscription> subscriptions) =>
        BuildPlan(subscriptions).Where(r => r.NotifyAt.Date == DateTime.Today && r.NotifyAt <= DateTime.Now);

    public async Task RescheduleAsync(IEnumerable<Subscription> subscriptions)
    {
        try
        {
            var list = subscriptions.ToList();
            Markers.Cleanup();
            await PlatformRescheduleAsync(list);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Reminder scheduling failed: {ex}");
        }
    }

    /// <summary>Shows a test notification. Returns null on success or an error message.</summary>
    public async Task<string?> ShowTestAsync()
    {
        try
        {
            if (!await EnsurePermissionAsync())
                return "Notifications are blocked. Allow them for Subscription Tracker in your system settings.";

            Show(TestNotificationId, "🔔 Subscription Tracker",
                "Notifications are working. You'll be reminded before each payment.");
            return null;
        }
        catch (Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    // ---------------- Platform-specific part ----------------

#if WINDOWS
    private async Task PlatformRescheduleAsync(List<Subscription> subscriptions)
    {
        RemoveOldScheduledToasts();
        if (!_settings.NotificationsEnabled || !await EnsurePermissionAsync()) return;

        foreach (var r in DueNow(subscriptions))
            if (Markers.TryClaim("shown_" + r.Key))
                Show(r.NotificationId, r.Title, r.Body);
    }

    private static Task<bool> EnsurePermissionAsync() =>
        Task.FromResult(ToastNotificationManagerCompat.CreateToastNotifier().Setting == NotificationSetting.Enabled);

    private static void Show(int id, string title, string body) =>
        new ToastContentBuilder()
            .AddText(title)
            .AddText(body)
            .Show(t => t.Tag = id.ToString());

    /// <summary>Version 1.1 used scheduled toasts; the background process replaces them.</summary>
    private static void RemoveOldScheduledToasts()
    {
        var notifier = ToastNotificationManagerCompat.CreateToastNotifier();
        foreach (var toast in notifier.GetScheduledToastNotifications())
            notifier.RemoveFromSchedule(toast);
    }
#else
    private bool _permissionAsked;

    private async Task PlatformRescheduleAsync(List<Subscription> subscriptions)
    {
        LocalNotificationCenter.Current.CancelAll();
        if (!_settings.NotificationsEnabled || !await EnsurePermissionAsync()) return;

        var now = DateTime.Now;
        foreach (var r in BuildPlan(subscriptions))
        {
            if (r.NotifyAt > now)
            {
                // Scheduled by the system: arrives even if the app is closed
                await LocalNotificationCenter.Current.Show(new NotificationRequest
                {
                    NotificationId = r.NotificationId,
                    Title = r.Title,
                    Description = r.Body,
                    Schedule = new NotificationRequestSchedule { NotifyTime = r.NotifyAt },
                });
                Markers.TryClaim("scheduled_" + r.Key);
            }
            else if (r.NotifyAt.Date == DateTime.Today
                     && !Markers.Exists("scheduled_" + r.Key)       // the system already delivered it
                     && Markers.TryClaim("shown_" + r.Key))
            {
                Show(r.NotificationId, r.Title, r.Body);            // today's reminder was missed
            }
        }
    }

    private async Task<bool> EnsurePermissionAsync()
    {
        if (await LocalNotificationCenter.Current.AreNotificationsEnabled()) return true;
        if (_permissionAsked) return false;
        _permissionAsked = true;
        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    private static void Show(int id, string title, string body) =>
        LocalNotificationCenter.Current.Show(new NotificationRequest
        {
            NotificationId = id,
            Title = title,
            Description = body,
        });
#endif

    /// <summary>
    /// Tiny marker files ("this reminder was already shown").
    /// Creating a file is atomic, so two processes never show the same reminder twice.
    /// </summary>
    private static class Markers
    {
        private static string Dir => Path.Combine(FileSystem.AppDataDirectory, "reminders");

        public static bool Exists(string key) => File.Exists(Path.Combine(Dir, key));

        public static bool TryClaim(string key)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                using var _ = new FileStream(Path.Combine(Dir, key), FileMode.CreateNew);
                return true;
            }
            catch (IOException)
            {
                return false;   // already exists
            }
        }

        public static void Cleanup()
        {
            try
            {
                if (!Directory.Exists(Dir)) return;
                foreach (var file in Directory.GetFiles(Dir))
                    if (File.GetCreationTime(file) < DateTime.Now.AddDays(-60))
                        File.Delete(file);
            }
            catch
            {
                // not important
            }
        }
    }
}
