using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace ScratchDownloader.Views.Control;

public class AnimatedArrowBackground : Avalonia.Controls.Control
{
    private double _animationTime = 0;
    private readonly DispatcherTimer _timer;
    private Size _lastBoundsSize;

    // Snake movement parameters
    private const double SnakeSpeed = 9.0; // Pixels per frame
    private const double SnakeThickness = 8.0;
    private const double SnakeOpacity = 0.45;

    public static readonly StyledProperty<IBrush?> AccentBrushProperty =
        AvaloniaProperty.Register<AnimatedArrowBackground, IBrush?>(nameof(AccentBrush));

    public IBrush? AccentBrush
    {
        get => GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public AnimatedArrowBackground()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += (s, e) =>
        {
            _animationTime += SnakeSpeed;
            InvalidateVisual();
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        // Reset animation when control resizes
        if (Math.Abs(_lastBoundsSize.Width - bounds.Width) > 10 ||
            Math.Abs(_lastBoundsSize.Height - bounds.Height) > 10)
        {
            _animationTime = 0;
            _lastBoundsSize = bounds.Size;
        }

        // --- Build Waypoints for Snake Path ---
        double centerX = bounds.Width / 2;
        double topY = 30;
        double bottomY = bounds.Height - 40;
        double stepWidth = 60;  // How far left/right snake steps
        double stepHeight = 45; // Height of each zigzag step
        double arrowHeadSize = 50;

        var waypoints = new List<Point>();

        // 1. Zigzag path down the middle
        double currentY = topY;
        waypoints.Add(new Point(centerX, currentY));

        bool moveRight = true;
        double shaftEndY = bottomY - arrowHeadSize;

        while (currentY + stepHeight < shaftEndY)
        {
            currentY += stepHeight / 2;
            waypoints.Add(new Point(centerX, currentY)); // Move down

            double targetX = moveRight ? centerX + stepWidth : centerX - stepWidth;
            waypoints.Add(new Point(targetX, currentY)); // Move sideways

            currentY += stepHeight / 2;
            waypoints.Add(new Point(targetX, currentY)); // Move down

            waypoints.Add(new Point(centerX, currentY)); // Move back to center
            moveRight = !moveRight; // Alternate directions
        }

        // Ensure we end shaft precisely at top of arrow head
        waypoints.Add(new Point(centerX, shaftEndY));

        // 2. Snake forming the Arrow Head
        Point lineEnd = new Point(centerX, shaftEndY);
        Point arrowLeft = new Point(centerX - arrowHeadSize, shaftEndY);
        Point arrowTip = new Point(centerX, bottomY);
        Point arrowRight = new Point(centerX + arrowHeadSize, shaftEndY);

        waypoints.Add(arrowLeft);  // Left wing
        waypoints.Add(arrowTip);   // Down to tip
        waypoints.Add(arrowRight); // Up to right wing
        waypoints.Add(lineEnd);    // Back to center shaft

        // --- Calculate Segment Distances ---
        var distances = new List<double>();
        double totalDistance = 0;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            double dist = Distance(waypoints[i], waypoints[i + 1]);
            distances.Add(dist);
            totalDistance += dist;
        }

        // Reset loop after path finishes + delay
        if (_animationTime > totalDistance + 120)
        {
            _animationTime = 0;
        }

        double remainingDistance = _animationTime;

        // --- Prepare Pen ---
        var baseBrush = AccentBrush as ISolidColorBrush ?? Brushes.DodgerBlue;
        var snakeBrush = new SolidColorBrush(baseBrush.Color, SnakeOpacity);
        var snakePen = new Pen(snakeBrush, SnakeThickness, lineJoin: PenLineJoin.Miter, lineCap: PenLineCap.Square);

        // --- Draw Snake Path ---
        var geometry = new StreamGeometry();
        using (var geoContext = geometry.Open())
        {
            geoContext.BeginFigure(waypoints[0], false);

            for (int i = 0; i < distances.Count; i++)
            {
                double segLength = distances[i];

                if (remainingDistance >= segLength)
                {
                    // Fully complete segment
                    geoContext.LineTo(waypoints[i + 1]);
                    remainingDistance -= segLength;
                }
                else
                {
                    // Partial progress into current segment
                    double progress = remainingDistance / segLength;
                    Point partialPoint = Interpolate(waypoints[i], waypoints[i + 1], progress);
                    geoContext.LineTo(partialPoint);
                    break;
                }
            }

            geoContext.EndFigure(false);
            context.DrawGeometry(null, snakePen, geometry);
        }
    }

    private static double Distance(Point p1, Point p2)
    {
        double dx = p2.X - p1.X;
        double dy = p2.Y - p1.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Point Interpolate(Point p1, Point p2, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new Point(
            p1.X + (p2.X - p1.X) * t,
            p1.Y + (p2.Y - p1.Y) * t
        );
    }
}