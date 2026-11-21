namespace SubscriptionTracker.Controls;

/// <summary>
/// Compact (phone, narrow window) or regular (PC, tablet) layout, chosen by the page width —
/// not by the device type, so a narrow window on a PC looks exactly like a phone.
/// Sizes are published as app resources and used in XAML via {DynamicResource ...}.
/// </summary>
public static class SizeClass
{
    /// <summary>Pages narrower than this (in device-independent units) use the compact layout.</summary>
    public const double CompactMaxWidth = 600;

    private static ResourceDictionary? _resources;

    public static bool IsCompact { get; private set; }

    /// <summary>Raised when the layout switches between compact and regular.</summary>
    public static event EventHandler? Changed;

    /// <summary>Sets the initial values (before the first page is measured).</summary>
    public static void Init(ResourceDictionary resources)
    {
        _resources = resources;
        Apply(DeviceInfo.Idiom == DeviceIdiom.Phone);
    }

    public static void Update(double pageWidth)
    {
        if (double.IsNaN(pageWidth) || pageWidth <= 0) return;
        var compact = pageWidth < CompactMaxWidth;
        if (compact == IsCompact) return;
        Apply(compact);
        Changed?.Invoke(null, EventArgs.Empty);
    }

    private static void Apply(bool compact)
    {
        IsCompact = compact;
        var r = _resources;
        if (r is null) return;

        r["PagePadding"] = compact ? new Thickness(16, 4, 16, 24) : new Thickness(28, 8, 28, 32);
        r["CardPadding"] = new Thickness(compact ? 14 : 20);
        r["HeroPadding"] = new Thickness(compact ? 18 : 26);
        r["SectionSpacing"] = compact ? 12d : 16d;
        r["HeroNumberSize"] = compact ? 32d : 40d;
        r["StatNumberSize"] = compact ? 20d : 26d;
        r["CalendarCellHeight"] = compact ? 54d : 66d;
        r["CalendarIconSize"] = compact ? 14d : 16d;
        r["CalendarCardPadding"] = new Thickness(compact ? 10 : 18);
        r["WideOnly"] = !compact;
        r["CompactOnly"] = compact;
    }
}
