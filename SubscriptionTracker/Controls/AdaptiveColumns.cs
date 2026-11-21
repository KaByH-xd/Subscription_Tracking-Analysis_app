using Microsoft.Maui.Layouts;

namespace SubscriptionTracker.Controls;

/// <summary>
/// Responsive layout for cards.
/// Wide screen (PC, tablet in landscape): two independent columns; each child picks its column
/// with <c>c:AdaptiveColumns.Column="0|1"</c>.
/// Narrow screen (phone, small window): one column, children in their declaration order.
/// </summary>
public class AdaptiveColumns : Layout
{
    public static readonly BindableProperty ColumnProperty = BindableProperty.CreateAttached(
        "Column", typeof(int), typeof(AdaptiveColumns), 0, propertyChanged: OnChildLayoutChanged);

    public static int GetColumn(BindableObject view) => (int)view.GetValue(ColumnProperty);
    public static void SetColumn(BindableObject view, int value) => view.SetValue(ColumnProperty, value);

    public static readonly BindableProperty BreakpointProperty = BindableProperty.Create(
        nameof(Breakpoint), typeof(double), typeof(AdaptiveColumns), 820d, propertyChanged: OnLayoutChanged);

    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), typeof(double), typeof(AdaptiveColumns), 16d, propertyChanged: OnLayoutChanged);

    public static readonly BindableProperty FirstColumnShareProperty = BindableProperty.Create(
        nameof(FirstColumnShare), typeof(double), typeof(AdaptiveColumns), 0.5d, propertyChanged: OnLayoutChanged);

    /// <summary>Minimum width (without padding) at which two columns are used.</summary>
    public double Breakpoint
    {
        get => (double)GetValue(BreakpointProperty);
        set => SetValue(BreakpointProperty, value);
    }

    /// <summary>Gap between cards and between the columns.</summary>
    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <summary>Width share of the first column (0.5 = equal columns).</summary>
    public double FirstColumnShare
    {
        get => (double)GetValue(FirstColumnShareProperty);
        set => SetValue(FirstColumnShareProperty, value);
    }

    protected override ILayoutManager CreateLayoutManager() => new Manager(this);

    private static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((AdaptiveColumns)bindable).InvalidateMeasure();

    private static void OnChildLayoutChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is Element { Parent: AdaptiveColumns layout })
            layout.InvalidateMeasure();
    }

    private sealed class Manager : LayoutManager
    {
        private readonly AdaptiveColumns _layout;

        public Manager(AdaptiveColumns layout) : base(layout) => _layout = layout;

        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var padding = _layout.Padding;
            var width = widthConstraint - padding.HorizontalThickness;
            var height = Place(width, arrange: false, origin: Point.Zero);
            return new Size(widthConstraint, height + padding.VerticalThickness);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            var padding = _layout.Padding;
            var width = bounds.Width - padding.HorizontalThickness;
            var height = Place(width, arrange: true, origin: new Point(bounds.X + padding.Left, bounds.Y + padding.Top));
            return new Size(bounds.Width, height + padding.VerticalThickness);
        }

        /// <summary>Measures (and optionally arranges) the children; returns the content height.</summary>
        private double Place(double width, bool arrange, Point origin)
        {
            var spacing = _layout.Spacing;
            var twoColumns = !double.IsInfinity(width) && width >= _layout.Breakpoint;

            var firstWidth = twoColumns ? (width - spacing) * _layout.FirstColumnShare : width;
            var secondWidth = twoColumns ? width - spacing - firstWidth : width;
            double[] y = { 0, 0 };
            bool[] started = { false, false };

            foreach (var child in _layout)
            {
                if (child.Visibility == Visibility.Collapsed) continue;

                var col = twoColumns && GetColumn((BindableObject)child) == 1 ? 1 : 0;
                var colWidth = col == 0 ? firstWidth : secondWidth;
                var x = col == 0 ? 0 : firstWidth + spacing;

                if (started[col]) y[col] += spacing;
                started[col] = true;

                var size = arrange ? child.DesiredSize : child.Measure(colWidth, double.PositiveInfinity);
                if (arrange)
                    child.Arrange(new Rect(origin.X + x, origin.Y + y[col], colWidth, size.Height));

                y[col] += size.Height;
            }

            return Math.Max(y[0], y[1]);
        }
    }
}
