using SubscriptionTracker.ViewModels;

namespace SubscriptionTracker.Views;

public partial class CalendarPage : ContentPage
{
    private readonly CalendarViewModel _vm;

    public CalendarPage(CalendarViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        // Fewer logos fit in a day cell on a phone
        Controls.SizeClass.Changed += (_, _) => _vm.Refresh();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);
    }
}
