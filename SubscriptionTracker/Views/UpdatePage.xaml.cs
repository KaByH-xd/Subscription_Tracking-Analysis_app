using SubscriptionTracker.Services;
using SubscriptionTracker.ViewModels;

namespace SubscriptionTracker.Views;

public partial class UpdatePage : ContentPage
{
    public UpdatePage(UpdateService updates, UpdateInfo info)
    {
        InitializeComponent();
        BindingContext = new UpdateViewModel(updates, info, () => Navigation.PopModalAsync());
    }

    /// <summary>Checks GitHub and, if a newer version exists, shows the update dialog.</summary>
    public static async Task CheckAndShowAsync(UpdateService updates, bool force)
    {
        var info = await updates.CheckAsync(force);
        if (info is null || Shell.Current is null) return;
        await Shell.Current.Navigation.PushModalAsync(new UpdatePage(updates, info));
    }
}
