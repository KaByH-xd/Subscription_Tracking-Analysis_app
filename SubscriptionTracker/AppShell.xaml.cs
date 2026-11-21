using SubscriptionTracker.Services;
using SubscriptionTracker.Views;

namespace SubscriptionTracker;

public partial class AppShell : Shell
{
    /// <summary>Below this window width (Windows) the side menu collapses into a hamburger menu.</summary>
    private const double SideMenuMinWidth = 760;

    /// <summary>The app sections: title, route, page, icon name (nav_*.png / tile_*.png).</summary>
    private static readonly (string Title, string Route, Type Page, string Icon)[] Sections =
    {
        ("Home",          nameof(MainPage),          typeof(MainPage),          "home"),
        ("Subscriptions", nameof(SubscriptionsPage), typeof(SubscriptionsPage), "subscriptions"),
        ("Calendar",      nameof(CalendarPage),      typeof(CalendarPage),      "calendar"),
        ("Statistics",    nameof(StatisticsPage),    typeof(StatisticsPage),    "statistics"),
        ("Settings",      nameof(SettingsPage),      typeof(SettingsPage),      "settings"),
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
        SizeChanged += (_, _) => UpdateSideMenuMode();
    }

    /// <summary>Phone/tablet: bottom tab bar (icons are tinted by the system).</summary>
    private void BuildTabBar()
    {
        var tabs = new TabBar();
        foreach (var s in Sections)
        {
            tabs.Items.Add(new ShellContent
            {
                Title = s.Title,
                Route = s.Route,
                Icon = $"nav_{s.Icon}.png",
                ContentTemplate = new DataTemplate(s.Page)
            });
        }
        Items.Add(tabs);
    }

    /// <summary>Wide window → menu always visible; narrow window → menu opens from the hamburger button.</summary>
    private void UpdateSideMenuMode()
    {
        if (Width <= 0) return;
        var mode = Width >= SideMenuMinWidth ? FlyoutBehavior.Locked : FlyoutBehavior.Flyout;
        if (FlyoutBehavior != mode)
            FlyoutBehavior = mode;
    }

    // A few seconds after start the app checks GitHub for a newer version
    private async void OnLoaded(object? sender, EventArgs e)
    {
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
