using Microsoft.Maui.Handlers;

namespace SubscriptionTracker.Controls;

/// <summary>
/// Removes the native frame of text boxes and drop-downs (the underline on Android,
/// the border on Windows). The rounded "Field" border from Styles.xaml is drawn instead,
/// so inputs look the same on both platforms.
/// </summary>
public static class InputStyling
{
    public static void Apply()
    {
#if ANDROID
        EntryHandler.Mapper.AppendToMapping("Flat", (h, _) => h.PlatformView.Background = null);
        PickerHandler.Mapper.AppendToMapping("Flat", (h, _) => h.PlatformView.Background = null);
        DatePickerHandler.Mapper.AppendToMapping("Flat", (h, _) => h.PlatformView.Background = null);
#elif WINDOWS
        EntryHandler.Mapper.AppendToMapping("Flat", (h, _) =>
        {
            h.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            Transparent(h.PlatformView.Resources,
                "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused",
                "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused");
        });
        PickerHandler.Mapper.AppendToMapping("Flat", (h, _) =>
        {
            h.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            h.PlatformView.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
            Transparent(h.PlatformView.Resources,
                "ComboBoxBackground", "ComboBoxBackgroundPointerOver", "ComboBoxBackgroundPressed",
                "ComboBoxBackgroundFocused",
                "ComboBoxBorderBrush", "ComboBoxBorderBrushPointerOver", "ComboBoxBorderBrushPressed",
                "ComboBoxBorderBrushFocused");
        });
        DatePickerHandler.Mapper.AppendToMapping("Flat", (h, _) =>
        {
            h.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            h.PlatformView.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
            Transparent(h.PlatformView.Resources,
                "CalendarDatePickerBackground", "CalendarDatePickerBackgroundPointerOver",
                "CalendarDatePickerBackgroundPressed",
                "CalendarDatePickerBorderBrush", "CalendarDatePickerBorderBrushPointerOver",
                "CalendarDatePickerBorderBrushPressed");
        });
#endif
    }

#if WINDOWS
    /// <summary>
    /// Overrides the control's theme brushes with transparent ones. WinUI throws for resource keys it does not
    /// know, so every key is set separately: a failure only skips that cosmetic override
    /// instead of breaking the whole page.
    /// </summary>
    private static void Transparent(Microsoft.UI.Xaml.ResourceDictionary resources, params string[] keys)
    {
        foreach (var key in keys)
        {
            try
            {
                resources[key] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InputStyling: cannot override {key}: {ex.Message}");
            }
        }
    }
#endif
}
