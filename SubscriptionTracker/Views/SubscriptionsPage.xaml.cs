using SubscriptionTracker.Controls;
using SubscriptionTracker.ViewModels;

namespace SubscriptionTracker.Views;

public partial class SubscriptionsPage : ContentPage
{
    private const double MaxContentWidth = 1180;
    private const double MinCardWidth = 380;

    private readonly SubscriptionsViewModel _vm;
    private bool? _compactFilters;

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

    /// <summary>Centres the content on wide screens, picks how many cards fit in a row and arranges the filters.</summary>
    private void UpdateLayout()
    {
        if (Width <= 0) return;

        var compact = Width < SizeClass.CompactMaxWidth;
        var side = Math.Max(compact ? 16 : 28, (Width - MaxContentWidth) / 2);
        Toolbar.Padding = new Thickness(side, 4, side, 8);
        List.Margin = new Thickness(side, 0);

        var content = Width - 2 * side;
        var span = Math.Clamp((int)(content / MinCardWidth), 1, 3);
        if (ListLayout.Span != span)
            ListLayout.Span = span;

        ArrangeFilters(compact);
    }

    /// <summary>
    /// Regular: Status | Category | Sort by in one row.
    /// Compact: Category across the full width, then Status | Sort by — long values are not cut off.
    /// </summary>
    private void ArrangeFilters(bool compact)
    {
        if (_compactFilters == compact) return;
        _compactFilters = compact;

        Filters.ColumnDefinitions = compact
            ? new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star))
            : new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star));
        Filters.RowDefinitions = compact
            ? new RowDefinitionCollection(new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto))
            : new RowDefinitionCollection(new RowDefinition(GridLength.Auto));

        Place(CategoryField, row: 0, column: compact ? 0 : 1, span: compact ? 2 : 1);
        Place(StatusField, row: compact ? 1 : 0, column: 0, span: 1);
        Place(SortField, row: compact ? 1 : 0, column: compact ? 1 : 2, span: 1);
    }

    private static void Place(BindableObject view, int row, int column, int span)
    {
        Grid.SetRow(view, row);
        Grid.SetColumn(view, column);
        Grid.SetColumnSpan(view, span);
    }
}
