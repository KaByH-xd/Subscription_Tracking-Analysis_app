using SubscriptionTracker.Services;
using SubscriptionTracker.Views;

namespace SubscriptionTracker;

public partial class AppShell : Shell
{
    /// <summary>Below this window width (Windows) the side menu collapses into a hamburger menu.</summary>
    private const double SideMenuMinWidth = 760;

    /// <summary>
    /// The app sections: title, short title for the phone tab bar (5 tabs share ~360 dp),
    /// route, page, icon name (nav_*.png / tile_*.png).
    /// </summary>
    private static readonly (string Title, string TabTitle, string Route, Type Page, string Icon)[] Sections =
    {
        ("Home",          "Home",     nameof(MainPage),          typeof(MainPage),          "home"),
        ("Subscriptions", "Subs",     nameof(SubscriptionsPage), typeof(SubscriptionsPage), "subscriptions"),
        ("Calendar",      "Calendar", nameof(CalendarPage),      typeof(CalendarPage),      "calendar"),
        ("Statistics",    "Stats",    nameof(StatisticsPage),    typeof(StatisticsPage),    "statistics"),
        ("Settings",      "Settings", nameof(SettingsPage),      typeof(SettingsPage),      "settings"),
    };

    private bool _updateChecked;

    public AppShell()
    {
        InitializeComponent();
        VersionLabel.Text = $"Version {AppVersion.Text}";

        if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            BuildSideMenu();
        else
            BuildTabBar();

        Routing.RegisterRoute(nameof(EditSubscriptionPage), typeof(EditSubscriptionPage));
        Navigated += (_, _) => TrackPageWidth(CurrentPage);
        Loaded += OnLoaded;
    }

    /// <summary>PC: side menu with coloured icons.</summary>
    private void BuildSideMenu()
    {
        foreach (var s in Sections)
        {
            Items.Add(new FlyoutItem
            {
                Title = s.Title,
                Icon = $"tile_{s.Icon}.png",
                Items =
                {
                    new ShellContent
                    {
                        Title = s.Title,
                        Route = s.Route,
                        ContentTemplate = new DataTemplate(s.Page)
                    }
                }
            });
        }

        FlyoutBehavior = FlyoutBehavior.Locked;
        _hasSideMenu = true;
    }

    private bool _hasSideMenu;
    private Window? _window;

    // The Shell itself reports no size on Windows, and MAUI's Window.SizeChanged does not fire there,
    // so the side menu follows the native WinUI window.
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (!_hasSideMenu || propertyName != nameof(Window) || ReferenceEquals(_window, Window)) return;

        if (_window is not null) _window.HandlerChanged -= OnWindowHandlerChanged;
        _window = Window;
        if (_window is null) return;
        _window.HandlerChanged += OnWindowHandlerChanged;
        OnWindowHandlerChanged(_window, EventArgs.Empty);
    }

    private void OnWindowHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if (_window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window native)
        {
            native.SizeChanged -= OnNativeWindowSizeChanged;
            native.SizeChanged += OnNativeWindowSizeChanged;
            UpdateSideMenuMode(native.Bounds.Width);
        }
#endif
    }

#if WINDOWS
    private void OnNativeWindowSizeChanged(object sender, Microsoft.UI.Xaml.WindowSizeChangedEventArgs e) =>
        UpdateSideMenuMode(e.Size.Width);
#endif

    private readonly HashSet<Page> _trackedPages = new();

    /// <summary>Every shown page reports its width, which picks the compact or regular layout.</summary>
    private void TrackPageWidth(Page? page)
    {
        if (page is null) return;
        if (_trackedPages.Add(page))
            page.SizeChanged += (_, _) => Controls.SizeClass.Update(page.Width);
        Controls.SizeClass.Update(page.Width);
    }

    /// <summary>Phone/tablet: bottom tab bar (icons are tinted by the system).</summary>
    private void BuildTabBar()
    {
        var tabs = new TabBar();
        foreach (var s in Sections)
        {
            tabs.Items.Add(new ShellContent
            {
                Title = s.TabTitle,
                Route = s.Route,
                Icon = $"nav_{s.Icon}.png",
                ContentTemplate = new DataTemplate(s.Page)
            });
        }
        Items.Add(tabs);
    }

    /// <summary>Wide window → menu always visible; narrow window → menu opens from the hamburger button.</summary>
    private void UpdateSideMenuMode(double width)
    {
        if (double.IsNaN(width) || width <= 0) return;
        var mode = width >= SideMenuMinWidth ? FlyoutBehavior.Locked : FlyoutBehavior.Flyout;
        if (FlyoutBehavior != mode)
            FlyoutBehavior = mode;
    }

    // A few seconds after start the app checks GitHub for a newer version
    private async void OnLoaded(object? sender, EventArgs e)
    {
        TrackPageWidth(CurrentPage);
        if (_updateChecked) return;
        _updateChecked = true;

        var services = Handler?.MauiContext?.Services;
        var settings = services?.GetService<SettingsService>();
        var updates = services?.GetService<UpdateService>();
        if (settings is null || updates is null || !settings.AutoUpdate) return;

        await Task.Delay(TimeSpan.FromSeconds(3));
        try
        {
            await UpdatePage.CheckAndShowAsync(updates, force: false);
        }
        catch
        {
            // offline or GitHub unavailable — try again next time
        }
    }
}
