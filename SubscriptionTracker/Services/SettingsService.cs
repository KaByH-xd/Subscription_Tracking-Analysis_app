using System.Text.Json;
using System.Text.Json.Nodes;

namespace SubscriptionTracker.Services;

/// <summary>
/// User settings stored in a small JSON file (settings.json in the app data folder).
/// The file is re-read on every access, so the Windows background process
/// always sees changes made in the app window.
/// </summary>
public class SettingsService
{
    public const string ThemeSystem = "System";
    public const string ThemeLight = "Light";
    public const string ThemeDark = "Dark";

    private static readonly object Sync = new();
    private static string FilePath => Path.Combine(FileSystem.AppDataDirectory, "settings.json");

    /// <summary>Monthly budget for subscriptions in UAH. 0 means no budget.</summary>
    public decimal MonthlyBudget
    {
        get => Get("monthly_budget", 0m);
        set => Set("monthly_budget", value);
    }

    /// <summary>Reminders are sent every day during this many days before a payment.</summary>
    public int RemindDays
    {
        get => Get("remind_days", 3);
        set => Set("remind_days", value);
    }

    public bool NotificationsEnabled
    {
        get => Get("notifications_enabled", true);
        set => Set("notifications_enabled", value);
    }

    /// <summary>Windows only: start in the background when the user signs in.</summary>
    public bool StartWithWindows
    {
        get => Get("start_with_windows", true);
        set => Set("start_with_windows", value);
    }

    /// <summary>"System", "Light" or "Dark".</summary>
    public string Theme
    {
        get => Get("theme", ThemeSystem);
        set => Set("theme", value);
    }

    public static AppTheme ToAppTheme(string theme) => theme switch
    {
        ThemeLight => AppTheme.Light,
        ThemeDark => AppTheme.Dark,
        _ => AppTheme.Unspecified
    };

    public static void ApplyTheme(string theme)
    {
        if (Application.Current is not null)
            Application.Current.UserAppTheme = ToAppTheme(theme);
    }

    // ---------------- Storage ----------------

    private static T Get<T>(string key, T fallback)
    {
        try
        {
            var node = Load()[key];
            return node is null ? fallback : node.GetValue<T>();
        }
        catch
        {
            return fallback;
        }
    }

    private static void Set<T>(string key, T value)
    {
        lock (Sync)
        {
            var obj = Load();
            obj[key] = JsonValue.Create(value);
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static JsonObject Load()
    {
        lock (Sync)
        {
            try
            {
                if (File.Exists(FilePath) && JsonNode.Parse(File.ReadAllText(FilePath)) is JsonObject obj)
                    return obj;
            }
            catch
            {
                // corrupted file -> defaults
            }
            return MigrateFromPreferences();
        }
    }

    /// <summary>Earlier versions stored settings in Preferences.</summary>
    private static JsonObject MigrateFromPreferences()
    {
        var obj = new JsonObject();
        try
        {
            if (Preferences.Default.ContainsKey("monthly_budget"))
                obj["monthly_budget"] = (decimal)Preferences.Default.Get("monthly_budget", 0.0);
            if (Preferences.Default.ContainsKey("remind_days"))
                obj["remind_days"] = Preferences.Default.Get("remind_days", 3);
            if (Preferences.Default.ContainsKey("notifications_enabled"))
                obj["notifications_enabled"] = Preferences.Default.Get("notifications_enabled", true);
            if (Preferences.Default.ContainsKey("theme"))
                obj["theme"] = Preferences.Default.Get("theme", ThemeSystem);
        }
        catch
        {
            // Preferences may be unavailable (e.g. in the background process) — use defaults
        }
        return obj;
    }
}
