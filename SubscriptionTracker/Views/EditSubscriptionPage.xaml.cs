using SubscriptionTracker.Controls;
using SubscriptionTracker.ViewModels;

namespace SubscriptionTracker.Views;

public partial class EditSubscriptionPage : ContentPage
{
    private bool? _compact;

    public EditSubscriptionPage(EditSubscriptionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        SizeChanged += (_, _) => ArrangePriceRow();
    }

    /// <summary>
    /// Regular: Price | Currency | Billing period in one row.
    /// Compact (phone): Price | Currency, and Billing period across the full width below.
    /// </summary>
    private void ArrangePriceRow()
    {
        if (Width <= 0) return;
        var compact = Width < SizeClass.CompactMaxWidth;
        if (_compact == compact) return;
        _compact = compact;

        PriceRow.ColumnDefinitions = compact
            ? new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(110)))
            : new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(120)),
                new ColumnDefinition(GridLength.Star));
        PriceRow.RowDefinitions = compact
            ? new RowDefinitionCollection(new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto))
            : new RowDefinitionCollection(new RowDefinition(GridLength.Auto));

        Grid.SetRow(BillingPeriodField, compact ? 1 : 0);
        Grid.SetColumn(BillingPeriodField, compact ? 0 : 2);
        Grid.SetColumnSpan(BillingPeriodField, compact ? 2 : 1);
    }
}
