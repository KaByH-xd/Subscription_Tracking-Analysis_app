using SubscriptionTracker.Views;

namespace SubscriptionTracker;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(EditSubscriptionPage), typeof(EditSubscriptionPage));
    }
}
