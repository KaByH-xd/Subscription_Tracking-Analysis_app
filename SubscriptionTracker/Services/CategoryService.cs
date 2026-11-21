using SubscriptionTracker.Models;

namespace SubscriptionTracker.Services;

/// <summary>Creates and deletes the user's own categories.</summary>
public class CategoryService
{
    private readonly SettingsService _settings;
    private readonly IDatabaseService _db;

    public CategoryService(SettingsService settings, IDatabaseService db)
    {
        _settings = settings;
        _db = db;
    }

    /// <summary>Loads the saved categories (called once at start-up).</summary>
    public static void Load(SettingsService settings) => Categories.SetCustom(settings.CustomCategories);

    /// <summary>Error text for an invalid name, or null when the name can be used.</summary>
    public static string? Validate(string? name)
    {
        name = name?.Trim();
        if (string.IsNullOrEmpty(name)) return "Please enter a name.";
        if (name.Length > Categories.MaxNameLength) return $"The name is too long (max {Categories.MaxNameLength} characters).";
        if (Categories.Exists(name)) return $"The category \"{name}\" already exists.";
        return null;
    }

    /// <summary>Asks for a name and an icon; returns the new category name, or null if cancelled.</summary>
    public async Task<string?> PromptAndAddAsync()
    {
        var page = Shell.Current;
        if (page is null) return null;

        string? name = "";
        while (true)
        {
            name = await page.DisplayPromptAsync("New category",
                "Name of the category, e.g. Fitness or News.",
                accept: "Next", cancel: "Cancel",
                placeholder: "Category name", maxLength: Categories.MaxNameLength,
                keyboard: Keyboard.Text, initialValue: name ?? "");
            if (name is null) return null;   // cancelled

            var error = Validate(name);
            if (error is null) break;
            await page.DisplayAlert("Can't add the category", error, "OK");
        }

        var icon = await page.DisplayActionSheet("Choose an icon", "Skip", null, Categories.IconChoices);
        if (string.IsNullOrEmpty(icon) || !Categories.IconChoices.Contains(icon))
            icon = Categories.DefaultCustomIcon;

        return Add(name.Trim(), icon);
    }

    public string Add(string name, string icon)
    {
        var list = _settings.CustomCategories;
        list.Add(new CustomCategory(name, icon));
        Save(list);
        return name;
    }

    /// <summary>Deletes a category after confirmation; its subscriptions move to "Other".</summary>
    public async Task<bool> ConfirmAndRemoveAsync(CustomCategory category)
    {
        var all = await _db.GetAllAsync();
        var used = all.Where(s => s.Category == category.Name).ToList();

        var message = used.Count == 0
            ? $"Delete the category \"{category.Name}\"?"
            : $"Delete the category \"{category.Name}\"? Its {used.Count} subscription(s) will be moved to \"{Categories.Other}\".";
        if (Shell.Current is null ||
            !await Shell.Current.DisplayAlert("Delete category", message, "Delete", "Cancel"))
            return false;

        foreach (var s in used)
        {
            s.Category = Categories.Other;
            await _db.SaveAsync(s);
        }

        Save(_settings.CustomCategories.Where(c => c.Name != category.Name).ToList());
        return true;
    }

    private void Save(List<CustomCategory> list)
    {
        _settings.CustomCategories = list;
        Categories.SetCustom(list);
    }
}
