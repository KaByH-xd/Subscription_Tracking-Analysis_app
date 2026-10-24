using SubscriptionTracker.ViewModels;

namespace SubscriptionTracker.Views;

public partial class EditSubscriptionPage : ContentPage
{
    public EditSubscriptionPage(EditSubscriptionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
