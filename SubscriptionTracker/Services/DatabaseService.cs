using SQLite;
using SubscriptionTracker.Models;

namespace SubscriptionTracker.Services;

/// <summary>Local SQLite storage via sqlite-net-pcl.</summary>
public class DatabaseService : IDatabaseService
{
    private const string DbFileName = "subscriptions.db";
    private readonly LogoService _logos;
    private SQLiteAsyncConnection? _db;

    public DatabaseService(LogoService logos) => _logos = logos;

    public static string DbPath => Path.Combine(FileSystem.AppDataDirectory, DbFileName);

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null) return _db;

        _db = new SQLiteAsyncConnection(DbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<Subscription>();
        return _db;
    }

    public async Task<List<Subscription>> GetAllAsync()
    {
        var db = await GetDbAsync();
        var list = await db.Table<Subscription>().ToListAsync();

        foreach (var s in list)
        {
            var changed = MigrateLegacyValues(s);
            // Past payment dates are moved to the next billing period automatically
            if (s.IsActive && s.AdvanceIfPast()) changed = true;
            if (changed) await db.UpdateAsync(s);
        }

        // Logos are cached on disk, so after the first download this is instant
        await Task.WhenAll(list.Select(async s =>
            s.LogoPath = await _logos.GetLogoPathAsync(s.Website ?? LogoService.GuessDomain(s.Name))));

        return list;
    }

    /// <summary>Converts values saved by the earlier Ukrainian version of the app.</summary>
    private static bool MigrateLegacyValues(Subscription s)
    {
        var changed = false;

        var period = s.BillingPeriod switch
        {
            "Щорічно" => Subscription.Yearly,
            "Щомісяця" => Subscription.Monthly,
            _ => s.BillingPeriod
        };
        if (period != s.BillingPeriod) { s.BillingPeriod = period; changed = true; }

        var category = Categories.Normalize(s.Category);
        if (category != s.Category) { s.Category = category; changed = true; }

        return changed;
    }

    public async Task SaveAsync(Subscription subscription)
    {
        var db = await GetDbAsync();
        if (subscription.Id == 0)
            await db.InsertAsync(subscription);
        else
            await db.UpdateAsync(subscription);
    }

    public async Task DeleteAsync(Subscription subscription)
    {
        var db = await GetDbAsync();
        await db.DeleteAsync(subscription);
    }

    public async Task DeleteAllAsync()
    {
        var db = await GetDbAsync();
        await db.DeleteAllAsync<Subscription>();
    }

    public async Task AddDemoDataAsync()
    {
        var db = await GetDbAsync();
        var t = DateTime.Today;
        await db.InsertAllAsync(new[]
        {
            new Subscription { Name = "Spotify",    Category = Categories.Music,    Price = 119,  Website = "spotify.com",  NextPaymentDate = t.AddDays(14), StartDate = t.AddMonths(-8),  Notes = "Student plan" },
            new Subscription { Name = "Netflix",    Category = Categories.Video,    Price = 299,  Website = "netflix.com",  NextPaymentDate = t.AddDays(20), StartDate = t.AddMonths(-14) },
            new Subscription { Name = "Game Pass",  Category = Categories.Gaming,   Price = 190,  Website = "xbox.com",  NextPaymentDate = t.AddDays(2),  StartDate = t.AddMonths(-3) },
            new Subscription { Name = "Google One", Category = Categories.Cloud,    Price = 79,   Website = "one.google.com",   NextPaymentDate = t.AddDays(11), StartDate = t.AddMonths(-20) },
            new Subscription { Name = "ChatGPT Plus", Category = Categories.Software, Price = 20, Currency = "USD", Website = "chatgpt.com", NextPaymentDate = t.AddDays(6), StartDate = t.AddMonths(-5) },
            new Subscription { Name = "Adobe",      Category = Categories.Software, Price = 1200, Website = "adobe.com", BillingPeriod = Subscription.Yearly, NextPaymentDate = t.AddDays(95), StartDate = t.AddMonths(-9) },
        });
    }
}
