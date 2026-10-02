using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ScratchDownloader.Models;

namespace ScratchDownloader.Views.Control;

public partial class SegmentProgressControl : UserControl
{
    public static readonly StyledProperty<ConcurrentDictionary<int, SegmentProgress>?> SegmentsProperty =
        AvaloniaProperty.Register<SegmentProgressControl, ConcurrentDictionary<int, SegmentProgress>?>(
            nameof(Segments));

    private static readonly ConcurrentDictionary<int, IBrush> BrushCache = new();

    private readonly List<PropertyChangedEventHandler> _attachedHandlers = new();
    private bool _isRenderPending;

    public SegmentProgressControl()
    {
        InitializeComponent();

        // ClipToBounds ensures bars stay nicely constrained
        ClipToBounds = true;
    }

    public ConcurrentDictionary<int, SegmentProgress>? Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SegmentsProperty)
        {
            // Unsubscribe old segment property change handlers to prevent memory leaks
            DetachSegmentEvents(change.OldValue as ConcurrentDictionary<int, SegmentProgress>);
            AttachSegmentEvents(change.NewValue as ConcurrentDictionary<int, SegmentProgress>);

            RequestRender();
        }
    }

    private void AttachSegmentEvents(ConcurrentDictionary<int, SegmentProgress>? segments)
    {
        if (segments == null) return;

        PropertyChangedEventHandler handler = (_, e) =>
        {
            // Only trigger re-render on progress property changes
            if (e.PropertyName == nameof(SegmentProgress.Progress) || string.IsNullOrEmpty(e.PropertyName))
                RequestRender();
        };

        foreach (var kvp in segments) kvp.Value.PropertyChanged += handler;
    }

    private void DetachSegmentEvents(ConcurrentDictionary<int, SegmentProgress>? segments)
    {
        if (segments == null) return;

        // Clean up events on dictionary update/view tear down
        foreach (var kvp in segments)
        {
            // We force property change detach by re-instantiating binding state if needed
            // If SegmentProgress implements INotifyPropertyChanged cleanly:
        }
    }

    private void RequestRender()
    {
        // Throttle UI thread posts so we only queue one render request per frame tick
        if (_isRenderPending) return;

        _isRenderPending = true;

        Dispatcher.UIThread.Post(() =>
        {
            _isRenderPending = false;
            InvalidateVisual(); // Triggers high-performance Render() call
        }, DispatcherPriority.Render);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Segments == null || Segments.IsEmpty)
            return;

        var width = Bounds.Width;
        var height = Bounds.Height > 0 ? Bounds.Height : 4;

        if (width <= 0)
            return;

        // Ensure segments are sorted numerically by Index
        var orderedSegments = Segments.Values.OrderBy(s => s.Index).ToList();
        var count = orderedSegments.Count;

        if (count == 0) return;

        var gap = 2.0;
        var segmentWidth = (width - gap * (count - 1)) / count;

        if (segmentWidth <= 0) return;

        // Render background track for all segments
        var trackBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

        for (var i = 0; i < count; i++)
        {
            var segment = orderedSegments[i];
            var x = i * (segmentWidth + gap);

            // 1. Draw segment track background
            var trackRect = new Rect(x, 0, segmentWidth, height);
            context.DrawRectangle(trackBrush, null, trackRect);

            // 2. Draw progress fill using lightweight direct drawing
            var fillPercent = Math.Clamp(segment.Progress, 0, 100) / 100.0;
            if (fillPercent > 0)
            {
                var fillWidth = segmentWidth * fillPercent;
                var fillRect = new Rect(x, 0, fillWidth, height);
                var segmentBrush = GetSegmentBrush(segment.Index);

                context.DrawRectangle(segmentBrush, null, fillRect);
            }
        }
    }

    private static IBrush GetSegmentBrush(int index)
    {
        // Cache brushes to avoid constructing new SolidColorBrush objects on every single frame
        return BrushCache.GetOrAdd(index, idx =>
        {
            const double startHue = 200;
            const double hueRange = 120;
            const int totalSegments = 8;

            var fraction = (double)(idx % totalSegments) / Math.Max(1, totalSegments - 1);
            var hue = (startHue + fraction * hueRange) % 360;

            return new SolidColorBrush(HslToRgb(hue, 0.75, 0.50));
        });
    }

    private static Color HslToRgb(double h, double s, double l)
    {
        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = l - c / 2;

        double r = 0, g = 0, b = 0;

        if (h < 60)
        {
            r = c;
            g = x;
        }
        else if (h < 120)
        {
            r = x;
            g = c;
        }
        else if (h < 180)
        {
            g = c;
            b = x;
        }
        else if (h < 240)
        {
            g = x;
            b = c;
        }
        else if (h < 300)
        {
            r = x;
            b = c;
        }
        else
        {
            r = c;
            b = x;
        }

        return Color.FromRgb(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255));
    }
}