using Microsoft.UI.Xaml;

namespace SubscriptionTracker.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (BackgroundMode.IsBackgroundLaunch)
        {
            // No window: just watch for upcoming payments and show reminders
            await BackgroundMode.RunAsync();
            Exit();
            return;
        }

        base.OnLaunched(args);
    }
}
