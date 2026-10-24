using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SubscriptionTracker.Services;

/// <summary>
/// Currencies and exchange rates to UAH.
/// Rates come from the National Bank of Ukraine open API and are cached in Preferences,
/// so totals still work offline.
/// </summary>
public static class CurrencyService
{
    public const string Base = "UAH";
    public static readonly string[] Codes = { "UAH", "USD", "EUR", "GBP", "PLN" };

    // Fallback rates (NBU, 26.10.2026) used until the first successful update
    private static readonly Dictionary<string, decimal> DefaultRates = new()
    {
        ["UAH"] = 1m, ["USD"] = 45.2021m, ["EUR"] = 50.5766m, ["GBP"] = 60.4307m, ["PLN"] = 11.5124m
    };

    private const string NbuUrl = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(6) };
    private static Dictionary<string, decimal>? _rates;

    public static IReadOnlyDictionary<string, decimal> Rates => _rates ??= Load();

    public static DateTime? LastUpdated
    {
        get
        {
            var ticks = Preferences.Default.Get("rates_updated", 0L);
            return ticks == 0 ? null : new DateTime(ticks);
        }
    }

    public static decimal ToUah(decimal amount, string? currency) =>
        amount * (Rates.TryGetValue(currency ?? Base, out var r) ? r : 1m);

    public static string Symbol(string? currency) => currency switch
    {
        "USD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        "PLN" => "zł",
        _ => "₴"
    };

    /// <summary>Formats an amount with its currency: "$9.99", "€12", "119 ₴", "25 zł".</summary>
    public static string Format(decimal amount, string? currency, string number = "0.##")
    {
        var value = amount.ToString(number, CultureInfo.CurrentCulture);
        return currency switch
        {
            "USD" or "EUR" or "GBP" => Symbol(currency) + value,
            _ => $"{value} {Symbol(currency)}"
        };
    }

    public static Task<bool> UpdateIfStaleAsync() =>
        LastUpdated is { } d && DateTime.Now - d < TimeSpan.FromHours(12)
            ? Task.FromResult(false)
            : UpdateFromNbuAsync();

    /// <summary>Downloads today's official rates from the NBU. Returns false when offline.</summary>
    public static async Task<bool> UpdateFromNbuAsync()
    {
        try
        {
            var items = await Http.GetFromJsonAsync<List<NbuRate>>(NbuUrl);
            if (items is null || items.Count == 0) return false;

            var rates = new Dictionary<string, decimal> { [Base] = 1m };
            foreach (var code in Codes.Where(c => c != Base))
            {
                var item = items.FirstOrDefault(i => i.Code == code);
                rates[code] = item?.Rate ?? Rates[code];
            }

            _rates = rates;
            foreach (var (code, rate) in rates)
                Preferences.Default.Set($"rate_{code}", (double)rate);
            Preferences.Default.Set("rates_updated", DateTime.Now.Ticks);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, decimal> Load()
    {
        var rates = new Dictionary<string, decimal>();
        foreach (var code in Codes)
        {
            var saved = Preferences.Default.Get($"rate_{code}", 0.0);
            rates[code] = saved > 0 ? (decimal)saved : DefaultRates[code];
        }
        return rates;
    }

    private sealed class NbuRate
    {
        [JsonPropertyName("cc")] public string Code { get; set; } = "";
        [JsonPropertyName("rate")] public decimal Rate { get; set; }
    }
}
