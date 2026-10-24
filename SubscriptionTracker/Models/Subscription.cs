using SQLite;
using SubscriptionTracker.Services;

namespace SubscriptionTracker.Models;

/// <summary>A paid subscription (table "Subscriptions" in SQLite).</summary>
[Table("Subscriptions")]
public class Subscription
{
    public const string Monthly = "Monthly";
    public const string Yearly = "Yearly";

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = "";

    public string Category { get; set; } = Categories.Other;
    public decimal Price { get; set; }
    public string Currency { get; set; } = CurrencyService.Base;
    public string BillingPeriod { get; set; } = Monthly;
    public DateTime NextPaymentDate { get; set; } = DateTime.Today.AddMonths(1);
    public DateTime StartDate { get; set; } = DateTime.Today;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    /// <summary>Service website, used to show its logo (e.g. "spotify.com").</summary>
    public string? Website { get; set; }

    // ---------- Computed properties (not stored in the database) ----------

    [Ignore] public bool IsYearly => BillingPeriod == Yearly;

    /// <summary>Monthly equivalent in the original currency (yearly price / 12).</summary>
    [Ignore] public decimal MonthlyPrice => IsYearly ? Price / 12 : Price;

    /// <summary>Price converted to UAH at the current rate.</summary>
    [Ignore] public decimal PriceUah => CurrencyService.ToUah(Price, Currency);

    /// <summary>Monthly equivalent in UAH — used for all totals.</summary>
    [Ignore] public decimal MonthlyPriceUah => CurrencyService.ToUah(MonthlyPrice, Currency);

    [Ignore] public bool IsForeignCurrency => Currency != CurrencyService.Base;

    [Ignore] public int DaysLeft => (NextPaymentDate.Date - DateTime.Today).Days;

    [Ignore] public string Icon => Categories.Icon(Category);

    /// <summary>Local path of the downloaded logo (filled by DatabaseService).</summary>
    [Ignore] public string? LogoPath { get; set; }
    [Ignore] public ImageSource? Logo => LogoPath is null ? null : ImageSource.FromFile(LogoPath);
    [Ignore] public bool HasLogo => LogoPath is not null;
    [Ignore] public bool ShowEmoji => LogoPath is null;

    [Ignore] public string AmountText => CurrencyService.Format(Price, Currency);

    [Ignore] public string PriceText => $"{AmountText} / {(IsYearly ? "yr" : "mo")}";

    /// <summary>"≈ 451 ₴" for foreign currencies, empty for UAH.</summary>
    [Ignore] public string UahHint => IsForeignCurrency ? $"≈ {PriceUah:N0} ₴" : "";

    [Ignore] public string MonthlyUahText => $"{MonthlyPriceUah:N0} ₴/mo";

    [Ignore] public string DaysLeftText => DaysLeft switch
    {
        0 => "today",
        1 => "tomorrow",
        _ => $"in {DaysLeft} days"
    };

    [Ignore] public string StatusText => IsActive
        ? $"Next payment: {NextPaymentDate:MMM d, yyyy} ({DaysLeftText})"
        : "⏸ Paused";

    [Ignore] public double ItemOpacity => IsActive ? 1.0 : 0.55;

    /// <summary>Moves a past payment date forward to the next billing period.</summary>
    public bool AdvanceIfPast()
    {
        var changed = false;
        while (NextPaymentDate.Date < DateTime.Today)
        {
            NextPaymentDate = IsYearly ? NextPaymentDate.AddYears(1) : NextPaymentDate.AddMonths(1);
            changed = true;
        }
        return changed;
    }

    /// <summary>All payment dates within [from; to] (used by the calendar).</summary>
    public IEnumerable<DateTime> PaymentsBetween(DateTime from, DateTime to)
    {
        var step = IsYearly ? 12 : 1;
        var baseDate = NextPaymentDate.Date;
        var diff = (from.Year - baseDate.Year) * 12 + from.Month - baseDate.Month;
        var k = (int)Math.Floor(diff / (double)step) - 1;

        while (true)
        {
            var d = baseDate.AddMonths(k * step);
            if (d > to.Date) yield break;
            if (d >= from.Date && d >= StartDate.Date) yield return d;
            k++;
        }
    }
}
