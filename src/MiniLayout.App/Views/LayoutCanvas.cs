using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MiniLayout.Core.Models;

namespace MiniLayout.App.Views;

/// <summary>
/// 2D top-down canvas rendering 3D layout components with collision status colors.
/// </summary>
public class LayoutCanvas : Control
{
    public static readonly StyledProperty<IEnumerable<LayoutComponent>?> ComponentsProperty =
        AvaloniaProperty.Register<LayoutCanvas, IEnumerable<LayoutComponent>?>(nameof(Components));

    public IEnumerable<LayoutComponent>? Components
    {
        get => GetValue(ComponentsProperty);
        set => SetValue(ComponentsProperty, value);
    }

    static LayoutCanvas()
    {
        AffectsRender<LayoutCanvas>(ComponentsProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ComponentsProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldObs)
            {
                oldObs.CollectionChanged -= OnCollectionChanged;
            }
            if (change.NewValue is INotifyCollectionChanged newObs)
            {
                newObs.CollectionChanged += OnCollectionChanged;
            }
            InvalidateVisual();
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        context.FillRectangle(new SolidColorBrush(Color.Parse("#18181f")), bounds);

        double centerX = bounds.Width * 0.5;
        double centerY = bounds.Height * 0.5;
        const double scale = 40.0; // 40 pixels per layout coordinate unit

        // Draw background grid lines
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#252530")), 1);
        for (double x = centerX % scale; x < bounds.Width; x += scale)
        {
            context.DrawLine(gridPen, new Point(x, 0), new Point(x, bounds.Height));
        }
        for (double y = centerY % scale; y < bounds.Height; y += scale)
        {
            context.DrawLine(gridPen, new Point(0, y), new Point(bounds.Width, y));
        }

        // Draw orthogonal origin axes
        var axisPen = new Pen(new SolidColorBrush(Color.Parse("#3f3f50")), 1.5);
        context.DrawLine(axisPen, new Point(0, centerY), new Point(bounds.Width, centerY));
        context.DrawLine(axisPen, new Point(centerX, 0), new Point(centerX, bounds.Height));

        if (Components == null)
        {
            return;
        }

        var clearBrush = new SolidColorBrush(Color.Parse("#2ecc71"));    // Green: clear
        var collideBrush = new SolidColorBrush(Color.Parse("#e74c3c"));  // Red: colliding
        var borderPen = new Pen(new SolidColorBrush(Color.Parse("#ffffff")), 1.2);
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);

        foreach (var component in Components)
        {
            double w = component.Bounds.Width * scale;
            double h = component.Bounds.Height * scale;
            double left = centerX + (component.Bounds.X * scale) - (w * 0.5);
            double top = centerY - (component.Bounds.Y * scale) - (h * 0.5);

            var rect = new Rect(left, top, w, h);
            var fill = component.IsColliding ? collideBrush : clearBrush;

            context.FillRectangle(fill, rect);
            context.DrawRectangle(borderPen, rect);

            string label = $"{component.Name} [#{component.Id}]";
            var formattedText = new FormattedText(
                label,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                11,
                Brushes.White);

            context.DrawText(formattedText, new Point(left + 6, top + 6));
        }
    }
}
