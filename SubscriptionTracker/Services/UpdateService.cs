using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace SubscriptionTracker.Services;

/// <summary>A newer version found on GitHub Releases.</summary>
public record UpdateInfo(Version Version, string AssetName, string DownloadUrl, string? Sha256, string ReleaseUrl)
{
    public string VersionText => Version.ToString(3);
}

/// <summary>
/// Self-update from GitHub Releases.
/// Windows: downloads the new installer and runs it silently (the app restarts by itself).
/// Android: downloads the new APK and opens the system installer (the user taps "Update").
/// Every download is checked against the SHA-256 checksum published by GitHub.
/// </summary>
public class UpdateService
{
    public const string Repository = "KaByH-xd/Subscription_Tracking-Analysis_app";
    public const string ReleasesPage = $"https://github.com/{Repository}/releases/latest";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private static readonly HttpClient Http = CreateClient();
    private readonly SettingsService _settings;

    public UpdateService(SettingsService settings) => _settings = settings;

#if WINDOWS || ANDROID
    public static bool IsSupported => true;
#else
    public static bool IsSupported => false;
#endif

    /// <summary>
    /// Returns information about a newer version, or null when the app is up to date.
    /// Without <paramref name="force"/> GitHub is asked at most once every 6 hours.
    /// Throws when GitHub can't be reached (callers decide whether to show the error).
    /// </summary>
    public async Task<UpdateInfo?> CheckAsync(bool force = false)
    {
        if (!IsSupported) return null;
        if (!force && DateTime.Now - _settings.LastUpdateCheck < CheckInterval) return null;

        var release = await Http.GetFromJsonAsync<GitHubRelease>(
            $"https://api.github.com/repos/{Repository}/releases/latest");
        _settings.LastUpdateCheck = DateTime.Now;

        if (release is null || release.Draft || release.Prerelease) return null;
        if (!Version.TryParse(release.TagName.TrimStart('v', 'V'), out var latest)) return null;
        latest = AppVersion.Normalize(latest);
        if (latest <= AppVersion.Current) return null;

        var asset = release.Assets.FirstOrDefault(a => IsAssetForThisPlatform(a.Name));
        if (asset is null) return null;

        var sha = asset.Digest is { } d && d.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? d[7..] : null;
        return new UpdateInfo(latest, asset.Name, asset.DownloadUrl, sha, release.HtmlUrl);
    }

    /// <summary>Downloads the installer/APK and verifies its checksum. Returns the local file path.</summary>
    public async Task<string> DownloadAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var dir = Path.Combine(FileSystem.CacheDirectory, "updates");
        Directory.CreateDirectory(dir);
        foreach (var old in Directory.GetFiles(dir)) TryDelete(old);
        var path = Path.Combine(dir, update.AssetName);

        using (var response = await Http.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? 0;

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            await using var target = File.Create(path);
            var buffer = new byte[81920];
            long done = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, ct)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), ct);
                done += read;
                if (total > 0) progress?.Report((double)done / total);
            }
        }

        if (update.Sha256 is { } expected)
        {
            await using var file = File.OpenRead(path);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(file, ct));
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            {
                TryDelete(path);
                throw new InvalidDataException("The downloaded file is damaged (checksum mismatch). Please try again.");
            }
        }

        progress?.Report(1);
        return path;
    }

#if WINDOWS
    private static bool IsAssetForThisPlatform(string name) =>
        name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && name.Contains("Setup", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the Inno Setup installer silently. It closes the running app, installs the new version
    /// over the old one and starts it again (with --background when updated by the background process).
    /// </summary>
    public static void StartInstaller(string installerPath, bool restartInBackground)
    {
        var args = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS";
        if (restartInBackground) args += " /LAUNCHARGS=--background";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(installerPath, args) { UseShellExecute = true });
    }

    /// <summary>Called from the app window: install and quit (the installer restarts the app).</summary>
    public Task InstallAsync(string path)
    {
        StartInstaller(path, restartInBackground: false);
        Application.Current?.Quit();
        return Task.CompletedTask;
    }
#elif ANDROID
    private static bool IsAssetForThisPlatform(string name) =>
        name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase);

    /// <summary>Opens the system installer; Android asks the user to confirm the update.</summary>
    public Task InstallAsync(string path) =>
        Launcher.Default.OpenAsync(new OpenFileRequest("Install update",
            new ReadOnlyFile(path, "application/vnd.android.package-archive")));
#else
    private static bool IsAssetForThisPlatform(string name) => false;
    public Task InstallAsync(string path) => Task.CompletedTask;
#endif

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SubscriptionTracker", AppVersion.Text));
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* in use or already gone */ }
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string TagName { get; set; } = "";
        [JsonPropertyName("html_url")] public string HtmlUrl { get; set; } = ReleasesPage;
        [JsonPropertyName("draft")] public bool Draft { get; set; }
        [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
        [JsonPropertyName("assets")] public List<GitHubAsset> Assets { get; set; } = new();
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("browser_download_url")] public string DownloadUrl { get; set; } = "";
        [JsonPropertyName("digest")] public string? Digest { get; set; }
    }
}
