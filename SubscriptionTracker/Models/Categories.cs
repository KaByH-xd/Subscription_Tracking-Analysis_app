namespace SubscriptionTracker.Models;

/// <summary>A category created by the user (stored in settings.json).</summary>
public record CustomCategory(string Name, string Icon);

public static class Categories
{
    public const string Music = "Music";
    public const string Video = "Video";
    public const string Gaming = "Gaming";
    public const string Cloud = "Cloud";
    public const string Software = "Software";
    public const string Education = "Education";
    public const string Other = "Other";

    public const int MaxNameLength = 24;
    public const string DefaultCustomIcon = "🏷️";

    public static readonly string[] BuiltIn = { Music, Video, Gaming, Cloud, Software, Education, Other };

    /// <summary>Icons offered for a new category.</summary>
    public static readonly string[] IconChoices =
    {
        "🏷️", "🏠", "🚗", "🍔", "💪", "🛒", "📰", "📱", "💳", "🎨", "🧠", "✈️", "🐾", "🎁", "🏥", "📦"
    };

    // Accent colours for user categories (chosen by name, so a category keeps its colour)
    private static readonly string[] CustomPalette =
    {
        "#E35D9C", "#3BB273", "#E0A100", "#5A8DEE", "#C76CF0", "#EF6B4A", "#22A6B3", "#8A6E4B"
    };

    private static List<CustomCategory> _custom = new();

    public static IReadOnlyList<CustomCategory> Custom => _custom;

    /// <summary>Every category for pickers: built-in ones, then the user's, "Other" last.</summary>
    public static string[] All =>
        BuiltIn.Where(c => c != Other)
            .Concat(_custom.Select(c => c.Name))
            .Append(Other)
            .ToArray();

    public static void SetCustom(IEnumerable<CustomCategory> custom) => _custom = custom.ToList();

    public static bool IsBuiltIn(string name) => BuiltIn.Contains(name);

    /// <summary>True if a built-in or user category already has this name (case-insensitive).</summary>
    public static bool Exists(string name) =>
        All.Any(c => string.Equals(c, name.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Icon(string category) => category switch
    {
        Music => "🎵",
        Video => "🎬",
        Gaming => "🎮",
        Cloud => "☁️",
        Software => "💻",
        Education => "📚",
        Other => "📦",
        _ => _custom.FirstOrDefault(c => c.Name == category)?.Icon ?? DefaultCustomIcon
    };

    /// <summary>Accent colour of a category (chart bars, logo placeholders).</summary>
    public static Color Color(string category) => Microsoft.Maui.Graphics.Color.FromArgb(category switch
    {
        Music => "#FF5C8A",
        Video => "#FF7A45",
        Gaming => "#7B61FF",
        Cloud => "#2D9CFF",
        Software => "#14B8A6",
        Education => "#F5A524",
        Other => "#8E95A8",
        _ => CustomPalette[StableHash(category) % CustomPalette.Length]
    });

    /// <summary>Same result on every run (string.GetHashCode is randomised per process).</summary>
    private static int StableHash(string text)
    {
        unchecked
        {
            var h = 17;
            foreach (var ch in text.ToLowerInvariant()) h = h * 31 + ch;
            return h & int.MaxValue;
        }
    }

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
