using System.Globalization;
using Plugin.LocalNotification;
using SubscriptionTracker.Services;
using SubscriptionTracker.ViewModels;
using SubscriptionTracker.Views;

namespace SubscriptionTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // English month names and number formatting
        var culture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Poppins-Regular.ttf", "PoppinsRegular");
                fonts.AddFont("Poppins-Medium.ttf", "PoppinsMedium");
                fonts.AddFont("Poppins-SemiBold.ttf", "PoppinsSemiBold");
                fonts.AddFont("Poppins-Bold.ttf", "PoppinsBold");
            });

        // Services
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<LogoService>();
        builder.Services.AddSingleton<ReminderService>();

        // ViewModels
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<SubscriptionsViewModel>();
        builder.Services.AddTransient<EditSubscriptionViewModel>();
        builder.Services.AddTransient<CalendarViewModel>();
        builder.Services.AddTransient<StatisticsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SubscriptionsPage>();
        builder.Services.AddTransient<EditSubscriptionPage>();
        builder.Services.AddTransient<CalendarPage>();
        builder.Services.AddTransient<StatisticsPage>();
        builder.Services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}
