using SubscriptionTracker.Services;

namespace SubscriptionTracker;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var settings = new SettingsService();

        // Restore the theme chosen in Settings (System / Light / Dark)
        UserAppTheme = SettingsService.ToAppTheme(settings.Theme);

#if WINDOWS
        // Background mode is on by default: reminders arrive on time even with the window closed
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
