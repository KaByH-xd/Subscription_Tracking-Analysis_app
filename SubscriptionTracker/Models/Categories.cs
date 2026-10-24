namespace SubscriptionTracker.Models;

public static class Categories
{
    public const string Music = "Music";
    public const string Video = "Video";
    public const string Gaming = "Gaming";
    public const string Cloud = "Cloud";
    public const string Software = "Software";
    public const string Education = "Education";
    public const string Other = "Other";

    public static readonly string[] All = { Music, Video, Gaming, Cloud, Software, Education, Other };

    public static string Icon(string category) => category switch
    {
        Music => "🎵",
        Video => "🎬",
        Gaming => "🎮",
        Cloud => "☁️",
        Software => "💻",
        Education => "📚",
        _ => "📦"
    };

    /// <summary>Maps category names saved by older (Ukrainian) versions of the app.</summary>
    public static string Normalize(string? category) => category switch
    {
        "Музика" => Music,
        "Відео" => Video,
        "Ігри" => Gaming,
        "Хмара" => Cloud,
        "ПЗ" => Software,
        "Освіта" => Education,
        "Інше" or null or "" => Other,
        _ => category
    };
}
