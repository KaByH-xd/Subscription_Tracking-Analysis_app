using SubscriptionTracker.ViewModels;

namespace SubscriptionTracker.Views;

public partial class SubscriptionsPage : ContentPage
{
    private const double MaxContentWidth = 1180;
    private const double MinCardWidth = 380;

    private readonly SubscriptionsViewModel _vm;

    public SubscriptionsPage(SubscriptionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        SizeChanged += (_, _) => UpdateLayout();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);
    }

    /// <summary>Centres the content on wide screens and picks how many cards fit in a row.</summary>
    private void UpdateLayout()
    {
        if (Width <= 0) return;

        var side = Math.Max(DeviceInfo.Idiom == DeviceIdiom.Phone ? 16 : 28, (Width - MaxContentWidth) / 2);
        Toolbar.Padding = new Thickness(side, 4, side, 8);
        List.Margin = new Thickness(side, 0);

        var content = Width - 2 * side;
        var span = Math.Clamp((int)(content / MinCardWidth), 1, 3);
        if (ListLayout.Span != span)
            ListLayout.Span = span;
    }
}
