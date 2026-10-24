using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubscriptionTracker.Models;
using SubscriptionTracker.Services;
using SubscriptionTracker.Views;

namespace SubscriptionTracker.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDatabaseService _db;
    private readonly SettingsService _settings;
    private readonly ReminderService _reminders;

    public DashboardViewModel(IDatabaseService db, SettingsService settings, ReminderService reminders)
    {
        _db = db;
        _settings = settings;
        _reminders = reminders;
    }

    public ObservableCollection<Subscription> Upcoming { get; } = new();
    public ObservableCollection<Subscription> Reminders { get; } = new();

    [ObservableProperty] private decimal monthlySpending;
    [ObservableProperty] private decimal yearlySpending;
    [ObservableProperty] private string summaryText = "";
    [ObservableProperty] private string nextPaymentText = "";

    [ObservableProperty] private bool hasBudget;
    [ObservableProperty] private double budgetProgress;
    [ObservableProperty] private string budgetText = "";
    [ObservableProperty] private Color budgetColor = Colors.Green;

    [ObservableProperty] private bool hasReminders;
    [ObservableProperty] private string remindersTitle = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        await CurrencyService.UpdateIfStaleAsync();   // refresh NBU rates at most twice a day
        var all = await _db.GetAllAsync();
        _ = _reminders.RescheduleAsync(all);
        var active = all.Where(s => s.IsActive).OrderBy(s => s.NextPaymentDate).ToList();

        MonthlySpending = active.Sum(s => s.MonthlyPriceUah);
        YearlySpending = MonthlySpending * 12;
        SummaryText = $"Active subscriptions: {active.Count}   •   ≈ {YearlySpending / 365:0.0} ₴ per day";

        var first = active.FirstOrDefault();
        NextPaymentText = first is null
            ? "No active subscriptions"
            : $"⚠️ Next charge: {first.Name} — {first.DaysLeftText}";

        Upcoming.Clear();
        foreach (var s in active.Take(5)) Upcoming.Add(s);

        // Reminders
        var days = _settings.RemindDays;
        Reminders.Clear();
        foreach (var s in active.Where(s => s.DaysLeft <= days)) Reminders.Add(s);
        HasReminders = Reminders.Count > 0;
        RemindersTitle = days == 1 ? "🔔 Due within 1 day" : $"🔔 Due within {days} days";

        // Budget
        var budget = _settings.MonthlyBudget;
        HasBudget = budget > 0;
        if (!HasBudget) return;

        BudgetProgress = (double)Math.Min(1m, MonthlySpending / budget);
        if (MonthlySpending <= budget)
        {
            BudgetColor = MonthlySpending / budget > 0.85m ? Color.FromArgb("#E08A00") : Color.FromArgb("#2E9E5B");
            BudgetText = $"{MonthlySpending:N0} / {budget:N0} ₴   •   {budget - MonthlySpending:N0} ₴ left";
        }
        else
        {
            BudgetColor = Color.FromArgb("#D93A3A");
            BudgetText = $"{MonthlySpending:N0} / {budget:N0} ₴   •   over budget by {MonthlySpending - budget:N0} ₴";
        }
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(nameof(EditSubscriptionPage));
}
