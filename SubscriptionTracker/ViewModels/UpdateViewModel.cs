using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubscriptionTracker.Services;

namespace SubscriptionTracker.ViewModels;

/// <summary>"A new version is available" dialog with download progress.</summary>
public partial class UpdateViewModel : ObservableObject
{
    private readonly UpdateService _updates;
    private readonly UpdateInfo _info;
    private readonly Func<Task> _close;

    public UpdateViewModel(UpdateService updates, UpdateInfo info, Func<Task> close)
    {
        _updates = updates;
        _info = info;
        _close = close;
    }

    public string Title => $"Version {_info.VersionText} is available";
    public string Subtitle => $"You have version {AppVersion.Text}.";

    public string Hint => DeviceInfo.Platform == DevicePlatform.Android
        ? "The update will be downloaded, then Android will ask you to confirm the installation."
        : "The update will be downloaded and installed automatically. The app will restart.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool isBusy;

    [ObservableProperty] private double progress;
    [ObservableProperty] private string status = "";

    public bool IsIdle => !IsBusy;

    [RelayCommand]
    private async Task UpdateNowAsync()
    {
        IsBusy = true;
        try
        {
            Status = "Downloading…";
            var progress = new Progress<double>(p =>
            {
                Progress = p;
                Status = $"Downloading… {p:P0}";
            });
            var path = await _updates.DownloadAsync(_info, progress);

            Status = DeviceInfo.Platform == DevicePlatform.Android
                ? "Confirm the installation in the window that opens."
                : "Installing… the app will restart in a moment.";
            await _updates.InstallAsync(path);
        }
        catch (Exception ex)
        {
            Status = $"Update failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task LaterAsync() => _close();

    [RelayCommand]
    private Task OpenReleaseNotesAsync() => Launcher.Default.OpenAsync(_info.ReleaseUrl);
}
