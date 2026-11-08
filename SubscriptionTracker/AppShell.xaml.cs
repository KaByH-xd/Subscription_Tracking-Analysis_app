using SubscriptionTracker.Services;
using SubscriptionTracker.Views;

namespace SubscriptionTracker;

public partial class AppShell : Shell
{
    private bool _updateChecked;

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(EditSubscriptionPage), typeof(EditSubscriptionPage));
        Loaded += OnLoaded;
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
