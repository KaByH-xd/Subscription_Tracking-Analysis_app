using SubscriptionTracker.ViewModels;

namespace SubscriptionTracker.Views;

public partial class MainPage : ContentPage
{
    private readonly DashboardViewModel _vm;

    public MainPage(DashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);
    }
}
