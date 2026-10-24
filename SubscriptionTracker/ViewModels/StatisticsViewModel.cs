using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubscriptionTracker.Models;
using SubscriptionTracker.Services;

namespace SubscriptionTracker.ViewModels;

public record CategoryStat(string Name, decimal Amount, double Share)
{
    public string Title => $"{Categories.Icon(Name)}  {Name}";
    public string AmountText => $"{Amount:N0} ₴";
    public string ShareText => $"{Share:P0}";
}

public partial class StatisticsViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    public StatisticsViewModel(IDatabaseService db) => _db = db;

    public ObservableCollection<CategoryStat> CategoryStats { get; } = new();
    public ObservableCollection<Subscription> TopSubscriptions { get; } = new();

    [ObservableProperty] private decimal perDay;
    [ObservableProperty] private decimal perMonth;
    [ObservableProperty] private decimal perYear;
    [ObservableProperty] private string insightText = "";
    [ObservableProperty] private string totalSpentText = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        var all = await _db.GetAllAsync();
        var active = all.Where(s => s.IsActive).ToList();

        PerMonth = active.Sum(s => s.MonthlyPriceUah);
        PerYear = PerMonth * 12;
        PerDay = PerYear / 365;

        CategoryStats.Clear();
        foreach (var g in active
                     .GroupBy(s => s.Category)
                     .Select(g => new { g.Key, Sum = g.Sum(s => s.MonthlyPriceUah) })
                     .OrderByDescending(x => x.Sum))
        {
            var share = PerMonth == 0 ? 0 : (double)(g.Sum / PerMonth);
            CategoryStats.Add(new CategoryStat(g.Key, g.Sum, share));
        }

        TopSubscriptions.Clear();
        foreach (var s in active.OrderByDescending(s => s.MonthlyPriceUah).Take(3))
            TopSubscriptions.Add(s);

        var top = CategoryStats.FirstOrDefault();
        InsightText = top is null
            ? "Add some subscriptions to see your analytics."
            : $"You spend about {PerYear:N0} ₴ a year on subscriptions. " +
              $"Your biggest category is {top.Name} ({top.ShareText}).";

        // Rough amount already paid since each subscription started
        var today = DateTime.Today;
        var spent = all.Sum(s =>
        {
            var months = Math.Max(0, (today.Year - s.StartDate.Year) * 12 + today.Month - s.StartDate.Month);
            return s.IsYearly ? s.PriceUah * (months / 12 + 1) : s.PriceUah * months;
        });
        TotalSpentText = $"Estimated total paid so far: {spent:N0} ₴";
    }
}
