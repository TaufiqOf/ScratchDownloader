using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace ScratchDownloader.Views.Control;

public class SnakeArrowBackground : Avalonia.Controls.Control
{
    private double _animationTime = 0;
    private readonly DispatcherTimer _timer;
    private Size _lastBoundsSize;
    private bool _hasFiredCompleted = false;

    private const double SnakeSpeed = 12.0; // Pixels per frame
    private const double SnakeThickness = 10.0;
    private const double SnakeOpacity = 0.55;

    public event EventHandler? AnimationCompleted;

    public static readonly StyledProperty<IBrush?> AccentBrushProperty =
        AvaloniaProperty.Register<SnakeArrowBackground, IBrush?>(nameof(AccentBrush));

    public static readonly StyledProperty<string?> CsvDataProperty =
        AvaloniaProperty.Register<SnakeArrowBackground, string?>(nameof(CsvData));

    public IBrush? AccentBrush
    {
        get => GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public string? CsvData
    {
        get => GetValue(CsvDataProperty);
        set => SetValue(CsvDataProperty, value);
    }

    static SnakeArrowBackground()
    {
        // Reset animation whenever new CSV data is assigned
        CsvDataProperty.Changed.AddClassHandler<SnakeArrowBackground>((x, e) => x.ResetAnimation());
    }

    public SnakeArrowBackground()
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

    public void ResetAnimation()
    {
        _animationTime = 0;
        _hasFiredCompleted = false;
        InvalidateVisual();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    { base.OnAttachedToVisualTree(e); _timer.Start(); }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { base.OnDetachedFromVisualTree(e); _timer.Stop(); }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        if (Math.Abs(_lastBoundsSize.Width - bounds.Width) > 10 ||
            Math.Abs(_lastBoundsSize.Height - bounds.Height) > 10)
        {
            _animationTime = 0;
            _lastBoundsSize = bounds.Size;
        }

        var waypoints = ParseCsvPath(CsvData, bounds.Width, bounds.Height);
        if (waypoints == null || waypoints.Count < 2) return;

        var distances = new List<double>();
        double totalDistance = 0;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            double dist = Distance(waypoints[i], waypoints[i + 1]);
            distances.Add(dist);
            totalDistance += dist;
        }

        double pauseTime = 60.0;
        double totalCycleTime = (totalDistance * 2) + (pauseTime * 2);

        // Check if full cycle (Draw + Pause + Wipe + Pause) finished
        if (_animationTime >= totalCycleTime)
        {
            if (!_hasFiredCompleted)
            {
                _hasFiredCompleted = true;
                AnimationCompleted?.Invoke(this, EventArgs.Empty);
            }
            _animationTime = 0; // Restart if CsvData wasn't changed
        }

        double headDistance = 0;
        double tailDistance = 0;

        if (_animationTime <= totalDistance)
        {
            // PHASE 1: Draw
            headDistance = _animationTime;
            tailDistance = 0;
        }
        else if (_animationTime <= totalDistance + pauseTime)
        {
            // PAUSE 1: Full shape visible
            headDistance = totalDistance;
            tailDistance = 0;
        }
        else if (_animationTime <= (totalDistance * 2) + pauseTime)
        {
            // PHASE 2: Wipe
            headDistance = totalDistance;
            tailDistance = _animationTime - totalDistance - pauseTime;
        }
        else
        {
            // PAUSE 2: Screen empty
            headDistance = totalDistance;
            tailDistance = totalDistance;
        }

        if (headDistance <= tailDistance) return;

        var activePoints = GetSubPath(waypoints, distances, tailDistance, headDistance);
        if (activePoints.Count < 2) return;

        var baseBrush = AccentBrush as ISolidColorBrush ?? Brushes.DodgerBlue;
        var snakeBrush = new SolidColorBrush(baseBrush.Color, SnakeOpacity);
        var snakePen = new Pen(snakeBrush, SnakeThickness, lineJoin: PenLineJoin.Miter, lineCap: PenLineCap.Square);

        var geometry = new StreamGeometry();
        using (var geoContext = geometry.Open())
        {
            geoContext.BeginFigure(activePoints[0], false);
            for (int i = 1; i < activePoints.Count; i++)
            {
                geoContext.LineTo(activePoints[i]);
            }
            geoContext.EndFigure(false);
            context.DrawGeometry(null, snakePen, geometry);
        }
    }

    private static List<Point> GetSubPath(List<Point> waypoints, List<double> distances, double startDist, double endDist)
    {
        var result = new List<Point>();
        double currentDist = 0;
        bool started = false;

        for (int i = 0; i < distances.Count; i++)
        {
            double segLength = distances[i];
            double segStart = currentDist;
            double segEnd = currentDist + segLength;

            if (segEnd >= startDist && segStart <= endDist)
            {
                if (!started)
                {
                    double tStart = Math.Clamp((startDist - segStart) / segLength, 0, 1);
                    result.Add(Interpolate(waypoints[i], waypoints[i + 1], tStart));
                    started = true;
                }

                if (segEnd >= endDist)
                {
                    double tEnd = Math.Clamp((endDist - segStart) / segLength, 0, 1);
                    result.Add(Interpolate(waypoints[i], waypoints[i + 1], tEnd));
                    break;
                }

                result.Add(waypoints[i + 1]);
            }

            currentDist += segLength;
        }

        return result;
    }

    private static List<Point>? ParseCsvPath(string? csvText, double viewWidth, double viewHeight)
    {
        if (string.IsNullOrWhiteSpace(csvText)) return null;

        var gridMap = new SortedDictionary<int, (int Row, int Col)>();
        int maxRows = 0;
        int maxCols = 0;

        using (var reader = new StringReader(csvText))
        {
            string? line;
            int r = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                maxCols = Math.Max(maxCols, parts.Length);

                for (int c = 0; c < parts.Length; c++)
                {
                    string token = parts[c].Trim();
                    if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int stepNumber))
                    {
                        gridMap[stepNumber] = (r, c);
                    }
                }
                r++;
            }
            maxRows = r;
        }

        if (gridMap.Count < 2 || maxRows == 0 || maxCols == 0) return null;

        double cellSize = Math.Min(viewWidth / maxCols, viewHeight / maxRows);
        double offsetX = (viewWidth - (maxCols * cellSize)) / 2;
        double offsetY = (viewHeight - (maxRows * cellSize)) / 2;

        var points = new List<Point>();
        foreach (var kvp in gridMap)
        {
            double x = offsetX + (kvp.Value.Col + 0.5) * cellSize;
            double y = offsetY + (kvp.Value.Row + 0.5) * cellSize;
            points.Add(new Point(x, y));
        }

        return points;
    }

    private static double Distance(Point p1, Point p2) => Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

    private static Point Interpolate(Point p1, Point p2, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new Point(p1.X + (p2.X - p1.X) * t, p1.Y + (p2.Y - p1.Y) * t);
    }
}