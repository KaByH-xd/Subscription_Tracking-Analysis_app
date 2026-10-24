using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubscriptionTracker.Services;

namespace SubscriptionTracker.ViewModels;

public record RateItem(string Code, decimal Rate)
{
    public string Text => $"1 {Code} = {Rate:0.00} ₴";
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly IDatabaseService _db;
    private readonly SettingsService _settings;
    private readonly ReminderService _reminders;
    private bool _loading;

    public SettingsViewModel(IDatabaseService db, SettingsService settings, ReminderService reminders)
    {
        _db = db;
        _settings = settings;
        _reminders = reminders;
    }

    public string[] RemindOptions { get; } = { "1 day", "3 days", "7 days" };
    public string RemindHint => $"You'll get a notification every day at {ReminderService.ReminderHour}:00 during this period (e.g. 3, 2 and 1 day before).";
    public string[] ThemeOptions { get; } = { SettingsService.ThemeSystem, SettingsService.ThemeLight, SettingsService.ThemeDark };
    public ObservableCollection<RateItem> Rates { get; } = new();

    [ObservableProperty] private string budgetText = "";
    [ObservableProperty] private string remindOption = "3 days";
    [ObservableProperty] private bool notificationsEnabled = true;
    [ObservableProperty] private bool startWithWindows = true;

    public bool IsWindows => DeviceInfo.Platform == DevicePlatform.WinUI;
    [ObservableProperty] private string theme = SettingsService.ThemeSystem;
    [ObservableProperty] private string ratesUpdatedText = "";
    [ObservableProperty] private bool isUpdatingRates;
    [ObservableProperty] private string dbPath = DatabaseService.DbPath;

    // The theme switches immediately, no need to press Save
    partial void OnThemeChanged(string value)
    {
        if (_loading) return;
        _settings.Theme = value;
        SettingsService.ApplyTheme(value);
    }

    partial void OnNotificationsEnabledChanged(bool value)
    {
        if (_loading) return;
        _settings.NotificationsEnabled = value;
        _ = RescheduleAsync();
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_loading) return;
        _settings.StartWithWindows = value;
#if WINDOWS
        WinUI.BackgroundMode.Apply(value);
#endif
    }

    [RelayCommand]
    private void Load()
    {
        _loading = true;
        BudgetText = _settings.MonthlyBudget > 0
            ? _settings.MonthlyBudget.ToString("0.##", CultureInfo.InvariantCulture)
            : "";
        RemindOption = _settings.RemindDays switch { 1 => "1 day", 7 => "7 days", _ => "3 days" };
        NotificationsEnabled = _settings.NotificationsEnabled;
        StartWithWindows = _settings.StartWithWindows;
        Theme = _settings.Theme;
        _loading = false;
        ShowRates();
    }

    private void ShowRates()
    {
        Rates.Clear();
        foreach (var code in CurrencyService.Codes.Where(c => c != CurrencyService.Base))
            Rates.Add(new RateItem(code, CurrencyService.Rates[code]));

        RatesUpdatedText = CurrencyService.LastUpdated is { } d
            ? $"National Bank of Ukraine rates, updated {d:MMM d, HH:mm}"
            : "Built-in rates (not updated yet)";
    }

    private async Task RescheduleAsync() => await _reminders.RescheduleAsync(await _db.GetAllAsync());

    [RelayCommand]
    private async Task SaveAsync()
    {
        var text = (BudgetText ?? "").Replace(',', '.').Trim();
        if (text.Length == 0)
        {
            _settings.MonthlyBudget = 0;
        }
        else if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var budget) && budget >= 0)
        {
            _settings.MonthlyBudget = budget;
        }
        else
        {
            await Shell.Current.DisplayAlert("Invalid budget", "Please enter a number, e.g. 1000.", "OK");
            return;
        }

        _settings.RemindDays = RemindOption switch { "1 day" => 1, "7 days" => 7, _ => 3 };
        await RescheduleAsync();
        await Shell.Current.DisplayAlert("Saved", "Your settings have been saved.", "OK");
    }

    [RelayCommand]
    private async Task UpdateRatesAsync()
    {
        IsUpdatingRates = true;
        var ok = await CurrencyService.UpdateFromNbuAsync();
        IsUpdatingRates = false;
        ShowRates();
        if (!ok)
            await Shell.Current.DisplayAlert("Offline", "Couldn't reach the National Bank of Ukraine. Saved rates are used.", "OK");
    }

    [RelayCommand]
    private async Task TestNotificationAsync()
    {
        var error = await _reminders.ShowTestAsync();
        if (error is not null)
            await Shell.Current.DisplayAlert("Notification failed", error, "OK");
    }

    [RelayCommand]
    private async Task AddDemoAsync()
    {
        await _db.AddDemoDataAsync();
        await RescheduleAsync();
        await Shell.Current.DisplayAlert("Done", "6 demo subscriptions were added.", "OK");
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        var ok = await Shell.Current.DisplayAlert("Delete all subscriptions",
            "This will permanently remove every subscription. Continue?", "Delete all", "Cancel");
        if (!ok) return;

        await _db.DeleteAllAsync();
        await RescheduleAsync();
    }
}
