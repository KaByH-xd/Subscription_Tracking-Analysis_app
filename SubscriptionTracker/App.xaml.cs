using SubscriptionTracker.Services;

namespace SubscriptionTracker;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Compact (phone) or regular sizes; updated later from the real page width
        Controls.SizeClass.Init(Resources);

        var settings = new SettingsService();

        // User-created categories are available everywhere from the start
        CategoryService.Load(settings);

        // Restore the theme chosen in Settings (System / Light / Dark)
        UserAppTheme = SettingsService.ToAppTheme(settings.Theme);

#if WINDOWS
        // Background mode is on by default: reminders arrive on time even with the window closed
        WinUI.BackgroundMode.MarkWindowOpen();
        WinUI.BackgroundMode.Apply(settings.StartWithWindows);
#endif
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell())
        {
            Title = "Subscription Tracker",
            Width = 1000,
            Height = 760,
            MinimumWidth = 420,
            MinimumHeight = 560
        };
}
