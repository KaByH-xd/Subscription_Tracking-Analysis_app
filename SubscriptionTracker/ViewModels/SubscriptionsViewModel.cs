using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubscriptionTracker.Models;
using SubscriptionTracker.Services;
using SubscriptionTracker.Views;

namespace SubscriptionTracker.ViewModels;

public partial class SubscriptionsViewModel : ObservableObject
{
    private const string AllCategories = "All categories";
    private const string StatusAll = "All statuses";
    private const string StatusActive = "Active";
    private const string StatusPaused = "Paused";
    private const string SortByDate = "By next payment";
    private const string SortByPrice = "By price";
    private const string SortByName = "By name";

    private readonly IDatabaseService _db;
    private List<Subscription> _all = new();

    public SubscriptionsViewModel(IDatabaseService db) => _db = db;

    public ObservableCollection<Subscription> Items { get; } = new();

    public string[] StatusFilters { get; } = { StatusAll, StatusActive, StatusPaused };
    /// <summary>Refreshed on every load, so categories created by the user appear here too.</summary>
    public ObservableCollection<string> CategoryFilters { get; } = new(new[] { AllCategories }.Concat(Categories.All));
    public string[] SortOptions { get; } = { SortByDate, SortByPrice, SortByName };

    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private string statusFilter = StatusAll;
    [ObservableProperty] private string categoryFilter = AllCategories;
    [ObservableProperty] private string sortOption = SortByDate;
    [ObservableProperty] private string countText = "";
    [ObservableProperty] private Subscription? selectedItem;

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnStatusFilterChanged(string value) => ApplyFilters();
    partial void OnCategoryFilterChanged(string value) => ApplyFilters();
    partial void OnSortOptionChanged(string value) => ApplyFilters();

    partial void OnSelectedItemChanged(Subscription? value)
    {
        if (value is null) return;
        SelectedItem = null;
        _ = Shell.Current.GoToAsync(nameof(EditSubscriptionPage),
            new Dictionary<string, object> { ["Subscription"] = value });
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        _all = await _db.GetAllAsync();
        RefreshCategoryFilters();
        ApplyFilters();
    }

    private void RefreshCategoryFilters()
    {
        var wanted = new[] { AllCategories }
            .Concat(Categories.All)
            .Concat(_all.Select(s => s.Category).Where(c => !Categories.Exists(c)))   // deleted custom ones still in use
            .Distinct()
            .ToList();
        if (wanted.SequenceEqual(CategoryFilters)) return;

        var current = CategoryFilter;
        CategoryFilters.Clear();
        foreach (var c in wanted) CategoryFilters.Add(c);
        CategoryFilter = wanted.Contains(current) ? current : AllCategories;
    }

    private void ApplyFilters()
    {
        IEnumerable<Subscription> q = _all;

        if (!string.IsNullOrWhiteSpace(SearchText))
            q = q.Where(s => s.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase));

        q = StatusFilter switch
        {
            StatusActive => q.Where(s => s.IsActive),
            StatusPaused => q.Where(s => !s.IsActive),
            _ => q
        };

        if (CategoryFilter != AllCategories)
            q = q.Where(s => s.Category == CategoryFilter);

        q = SortOption switch
        {
            SortByPrice => q.OrderByDescending(s => s.MonthlyPriceUah),
            SortByName => q.OrderBy(s => s.Name),
            _ => q.OrderByDescending(s => s.IsActive).ThenBy(s => s.NextPaymentDate)
        };

        Items.Clear();
        foreach (var s in q) Items.Add(s);
        CountText = $"Showing {Items.Count} of {_all.Count}";
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(nameof(EditSubscriptionPage));
}
