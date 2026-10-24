using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubscriptionTracker.Models;
using SubscriptionTracker.Services;

namespace SubscriptionTracker.ViewModels;

/// <summary>A payment on a specific date (used by the calendar).</summary>
public record PaymentItem(DateTime Date, Subscription Subscription)
{
    public string Title => Subscription.Name;
    public string DateText => Date.ToString("ddd, MMM d");
    public string AmountText => Subscription.AmountText;
}

/// <summary>One cell of the calendar grid.</summary>
public partial class CalendarDay : ObservableObject
{
    public CalendarDay(DateTime date, bool isCurrentMonth, List<PaymentItem> payments, Action<CalendarDay> onSelect)
    {
        Date = date;
        IsCurrentMonth = isCurrentMonth;
        Payments = payments;
        SelectCommand = new RelayCommand(() => onSelect(this));
    }

    public DateTime Date { get; }
    public bool IsCurrentMonth { get; }
    public List<PaymentItem> Payments { get; }
    public IRelayCommand SelectCommand { get; }

    public string DayText => Date.Day.ToString();
    public bool HasPayments => Payments.Count > 0;
    public bool IsToday => Date.Date == DateTime.Today;
    public string DayFont => IsToday || HasPayments ? "PoppinsSemiBold" : "PoppinsRegular";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Background))]
    [NotifyPropertyChangedFor(nameof(Foreground))]
    private bool isSelected;

    public Color Background =>
        IsSelected ? Color.FromArgb("#512BD4") :
        IsToday ? Color.FromArgb("#33512BD4") :
        Colors.Transparent;

    public Color Foreground =>
        IsSelected ? Colors.White :
        !IsCurrentMonth ? Color.FromArgb("#9090A0") :
        Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#F2F2F7") : Color.FromArgb("#1B1B24");
}

public partial class CalendarViewModel : ObservableObject
{
    private readonly IDatabaseService _db;
    private List<Subscription> _active = new();
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _selectedDate = DateTime.Today;

    public CalendarViewModel(IDatabaseService db) => _db = db;

    public ObservableCollection<CalendarDay> Days { get; } = new();
    public ObservableCollection<PaymentItem> SelectedPayments { get; } = new();
    public ObservableCollection<PaymentItem> MonthPayments { get; } = new();

    [ObservableProperty] private string monthTitle = "";
    [ObservableProperty] private string monthTotalText = "";
    [ObservableProperty] private string selectedTitle = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        _active = (await _db.GetAllAsync()).Where(s => s.IsActive).ToList();
        Build();
    }

    [RelayCommand]
    private void PrevMonth()
    {
        _month = _month.AddMonths(-1);
        _selectedDate = _month;
        Build();
    }

    [RelayCommand]
    private void NextMonth()
    {
        _month = _month.AddMonths(1);
        _selectedDate = _month;
        Build();
    }

    [RelayCommand]
    private void GoToToday()
    {
        _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _selectedDate = DateTime.Today;
        Build();
    }

    private void SelectDay(CalendarDay day)
    {
        if (!day.IsCurrentMonth)
        {
            _month = new DateTime(day.Date.Year, day.Date.Month, 1);
            _selectedDate = day.Date;
            Build();
            return;
        }

        foreach (var d in Days) d.IsSelected = d == day;
        _selectedDate = day.Date;
        ShowSelected(day);
    }

    private void Build()
    {
        MonthTitle = _month.ToString("MMMM yyyy");

        // Weeks start on Monday
        var offset = ((int)_month.DayOfWeek + 6) % 7;
        var start = _month.AddDays(-offset);
        var end = start.AddDays(41);

        var all = _active
            .SelectMany(s => s.PaymentsBetween(start, end).Select(d => new PaymentItem(d, s)))
            .ToList();

        Days.Clear();
        CalendarDay? selected = null;
        for (var i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            var day = new CalendarDay(date, date.Month == _month.Month,
                all.Where(p => p.Date == date).ToList(), SelectDay);

            if (date == _selectedDate.Date && day.IsCurrentMonth)
            {
                day.IsSelected = true;
                selected = day;
            }
            Days.Add(day);
        }

        var inMonth = all
            .Where(p => p.Date.Month == _month.Month && p.Date.Year == _month.Year)
            .OrderBy(p => p.Date)
            .ToList();

        MonthPayments.Clear();
        foreach (var p in inMonth) MonthPayments.Add(p);

        var count = inMonth.Count == 1 ? "1 charge" : $"{inMonth.Count} charges";
        MonthTotalText = $"Month total: {inMonth.Sum(p => p.Subscription.PriceUah):N0} ₴ ({count})";

        ShowSelected(selected);
    }

    private void ShowSelected(CalendarDay? day)
    {
        SelectedPayments.Clear();
        if (day is null)
        {
            SelectedTitle = "Select a day";
            return;
        }

        foreach (var p in day.Payments) SelectedPayments.Add(p);
        SelectedTitle = day.HasPayments
            ? $"Charges on {day.Date:MMMM d}:"
            : $"{day.Date:MMMM d} — no charges";
    }
}
