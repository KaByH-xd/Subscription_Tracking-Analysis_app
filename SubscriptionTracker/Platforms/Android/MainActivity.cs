using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace SubscriptionTracker;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        ApplySystemBars();
        if (Microsoft.Maui.Controls.Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => ApplySystemBars();
    }

    /// <summary>
    /// Status bar and navigation bar take the page / tab bar colours, with dark or light icons
    /// depending on the theme, so the app looks seamless from edge to edge.
    /// </summary>
    private void ApplySystemBars()
    {
        if (Window is null) return;
        var dark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;

#pragma warning disable CA1422 // deprecated on Android 15, where the bars are transparent anyway
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor(dark ? "#0E0F14" : "#F4F5FA"));
        Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor(dark ? "#191A22" : "#FFFFFF"));
#pragma warning restore CA1422

        var bars = WindowCompat.GetInsetsController(Window, Window.DecorView);
        bars.AppearanceLightStatusBars = !dark;
        bars.AppearanceLightNavigationBars = !dark;
    }
}
