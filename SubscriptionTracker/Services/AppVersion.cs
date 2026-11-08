using System.Reflection;

namespace SubscriptionTracker.Services;

/// <summary>The app version (set from ApplicationDisplayVersion at build time, e.g. "1.3.0").</summary>
public static class AppVersion
{
    public static Version Current { get; } = Read();

    public static string Text => Current.ToString(3);

    /// <summary>"1.3" -> "1.3.0", so versions compare correctly.</summary>
    public static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(0, v.Build));

    private static Version Read()
    {
        var info = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var text = info?.Split('+')[0];
        return Version.TryParse(text, out var v) ? Normalize(v) : new Version(0, 0, 0);
    }
}
