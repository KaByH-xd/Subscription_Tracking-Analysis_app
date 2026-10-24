using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubscriptionTracker.Models;
using SubscriptionTracker.Services;

namespace SubscriptionTracker.ViewModels;

/// <summary>Adds a new subscription or edits an existing one.</summary>
public partial class EditSubscriptionViewModel : ObservableObject, IQueryAttributable
{
    private readonly IDatabaseService _db;
    private readonly LogoService _logos;
    private readonly ReminderService _reminders;
    private Subscription _model = new();
    private string? _lastGuess;
    private int _logoRequest;

    public EditSubscriptionViewModel(IDatabaseService db, LogoService logos, ReminderService reminders)
    {
        _db = db;
        _logos = logos;
        _reminders = reminders;
    }

    public string[] CategoryList { get; } = Categories.All;
    public string[] Periods { get; } = { Subscription.Monthly, Subscription.Yearly };
    public string[] CurrencyList { get; } = CurrencyService.Codes;

    [ObservableProperty] private string pageTitle = "New subscription";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string category = Categories.Other;
    [ObservableProperty] private string priceText = "";
    [ObservableProperty] private string currency = CurrencyService.Base;
    [ObservableProperty] private string billingPeriod = Subscription.Monthly;
    [ObservableProperty] private DateTime nextPaymentDate = DateTime.Today.AddMonths(1);
    [ObservableProperty] private DateTime startDate = DateTime.Today;
    [ObservableProperty] private string? website;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private bool isEdit;
    [ObservableProperty] private string priceHint = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogoPreview))]
    [NotifyPropertyChangedFor(nameof(NoLogoPreview))]
    private ImageSource? logoPreview;

    public bool HasLogoPreview => LogoPreview is not null;
    public bool NoLogoPreview => LogoPreview is null;
    public string CategoryIcon => Categories.Icon(Category);

    partial void OnPriceTextChanged(string value) => UpdateHint();
    partial void OnBillingPeriodChanged(string value) => UpdateHint();
    partial void OnCurrencyChanged(string value) => UpdateHint();
    partial void OnCategoryChanged(string value) => OnPropertyChanged(nameof(CategoryIcon));

    // Typing "Netflix" fills in "netflix.com" automatically (unless the user typed their own website)
    partial void OnNameChanged(string value)
    {
        var guess = LogoService.GuessDomain(value);
        if (string.IsNullOrWhiteSpace(Website) || Website == _lastGuess)
            Website = guess;
        _lastGuess = guess;
    }

    partial void OnWebsiteChanged(string? value) => _ = UpdateLogoAsync(value);

    private async Task UpdateLogoAsync(string? domain)
    {
        var request = ++_logoRequest;
        var path = await _logos.GetLogoPathAsync(domain);
        if (request != _logoRequest) return;   // a newer request is in progress
        LogoPreview = path is null ? null : ImageSource.FromFile(path);
    }

    private void UpdateHint()
    {
        if (!TryParsePrice(out var p) || p <= 0) { PriceHint = ""; return; }

        var perMonth = BillingPeriod == Subscription.Yearly ? p / 12 : p;
        var hint = BillingPeriod == Subscription.Yearly
            ? $"≈ {CurrencyService.Format(perMonth, Currency)} per month"
            : $"≈ {CurrencyService.Format(p * 12, Currency)} per year";

        if (Currency != CurrencyService.Base)
            hint += $"   •   ≈ {CurrencyService.ToUah(perMonth, Currency):N0} ₴ per month";
        PriceHint = hint;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("Subscription", out var obj) || obj is not Subscription s) return;

        _model = s;
        IsEdit = true;
        PageTitle = "Edit subscription";
        Name = s.Name;
        Category = s.Category;
        Currency = s.Currency;
        PriceText = s.Price.ToString("0.##", CultureInfo.InvariantCulture);
        BillingPeriod = s.BillingPeriod;
        NextPaymentDate = s.NextPaymentDate;
        StartDate = s.StartDate;
        Website = s.Website ?? LogoService.GuessDomain(s.Name);
        Notes = s.Notes;
        IsActive = s.IsActive;
    }

    private bool TryParsePrice(out decimal price) =>
        decimal.TryParse((PriceText ?? "").Replace(',', '.').Trim(),
            NumberStyles.Number, CultureInfo.InvariantCulture, out price);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert("Missing name", "Please enter the service name.", "OK");
            return;
        }
        if (!TryParsePrice(out var price) || price <= 0)
        {
            await Shell.Current.DisplayAlert("Invalid price", "Please enter a valid price, e.g. 119 or 9.99.", "OK");
            return;
        }

        _model.Name = Name.Trim();
        _model.Category = Category;
        _model.Price = price;
        _model.Currency = Currency;
        _model.BillingPeriod = BillingPeriod;
        _model.NextPaymentDate = NextPaymentDate.Date;
        _model.StartDate = StartDate.Date;
        _model.Website = LogoService.NormalizeDomain(Website);
        _model.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        _model.IsActive = IsActive;

        await _db.SaveAsync(_model);
        _ = _reminders.RescheduleAsync(await _db.GetAllAsync());
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var ok = await Shell.Current.DisplayAlert("Delete subscription",
            $"Delete \"{_model.Name}\"? This cannot be undone.", "Delete", "Cancel");
        if (!ok) return;

        await _db.DeleteAsync(_model);
        _ = _reminders.RescheduleAsync(await _db.GetAllAsync());
        await Shell.Current.GoToAsync("..");
    }
}
