using System.Collections.Concurrent;

namespace SubscriptionTracker.Services;

/// <summary>
/// Loads service logos (website favicons) and caches them on disk,
/// so they are downloaded only once and also work offline.
/// </summary>
public class LogoService
{
    // Popular services -> website used to find the logo
    private static readonly Dictionary<string, string> KnownDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        ["spotify"] = "spotify.com",
        ["netflix"] = "netflix.com",
        ["youtube"] = "youtube.com",
        ["youtube premium"] = "youtube.com",
        ["youtube music"] = "music.youtube.com",
        ["game pass"] = "xbox.com",
        ["xbox"] = "xbox.com",
        ["xbox game pass"] = "xbox.com",
        ["playstation plus"] = "playstation.com",
        ["ps plus"] = "playstation.com",
        ["steam"] = "store.steampowered.com",
        ["google one"] = "one.google.com",
        ["google"] = "google.com",
        ["adobe"] = "adobe.com",
        ["apple music"] = "music.apple.com",
        ["icloud"] = "icloud.com",
        ["apple tv"] = "tv.apple.com",
        ["chatgpt"] = "chatgpt.com",
        ["openai"] = "openai.com",
        ["claude"] = "claude.ai",
        ["disney+"] = "disneyplus.com",
        ["disney plus"] = "disneyplus.com",
        ["hbo max"] = "max.com",
        ["max"] = "max.com",
        ["amazon prime"] = "amazon.com",
        ["prime video"] = "primevideo.com",
        ["megogo"] = "megogo.net",
        ["sweet.tv"] = "sweet.tv",
        ["sweet tv"] = "sweet.tv",
        ["kyivstar tv"] = "tv.kyivstar.ua",
        ["duolingo"] = "duolingo.com",
        ["coursera"] = "coursera.org",
        ["notion"] = "notion.so",
        ["figma"] = "figma.com",
        ["canva"] = "canva.com",
        ["github"] = "github.com",
        ["github copilot"] = "github.com",
        ["dropbox"] = "dropbox.com",
        ["microsoft 365"] = "microsoft.com",
        ["office 365"] = "microsoft.com",
        ["discord"] = "discord.com",
        ["discord nitro"] = "discord.com",
        ["telegram"] = "telegram.org",
        ["telegram premium"] = "telegram.org",
        ["twitch"] = "twitch.tv",
        ["deezer"] = "deezer.com",
        ["tidal"] = "tidal.com",
        ["midjourney"] = "midjourney.com",
        ["linkedin"] = "linkedin.com",
        ["jetbrains"] = "jetbrains.com",
    };

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(6) };
    private readonly ConcurrentDictionary<string, Task<string?>> _inFlight = new();
    private readonly string _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "logos");

    /// <summary>Guesses the website from the service name ("Netflix" -> "netflix.com").</summary>
    public static string? GuessDomain(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = name.Trim();
        if (KnownDomains.TryGetValue(key, out var domain)) return domain;

        // "Spotify Family" -> "spotify"
        var firstWord = key.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return KnownDomains.TryGetValue(firstWord, out domain) ? domain : null;
    }

    /// <summary>Normalizes user input: "https://www.Spotify.com/ua/" -> "spotify.com".</summary>
    public static string? NormalizeDomain(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim().ToLowerInvariant();
        if (!s.Contains("://")) s = "https://" + s;
        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri) || !uri.Host.Contains('.')) return null;
        var host = uri.Host;
        return host.StartsWith("www.") ? host[4..] : host;
    }

    /// <summary>Returns a local path to the logo PNG, or null if it can't be loaded.</summary>
    public Task<string?> GetLogoPathAsync(string? domain)
    {
        domain = NormalizeDomain(domain);
        if (domain is null) return Task.FromResult<string?>(null);
        return _inFlight.GetOrAdd(domain, LoadAsync);
    }

    private async Task<string?> LoadAsync(string domain)
    {
        try
        {
            Directory.CreateDirectory(_cacheDir);
            var file = Path.Combine(_cacheDir, domain.Replace('.', '_') + ".png");
            if (File.Exists(file) && new FileInfo(file).Length > 0) return file;

            var url = $"https://www.google.com/s2/favicons?domain={Uri.EscapeDataString(domain)}&sz=128";
            using var response = await Http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;   // unknown site -> show emoji instead

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length < 100) return null;

            await File.WriteAllBytesAsync(file, bytes);
            return file;
        }
        catch
        {
            _inFlight.TryRemove(domain, out _);   // offline: try again next time
            return null;
        }
    }
}
