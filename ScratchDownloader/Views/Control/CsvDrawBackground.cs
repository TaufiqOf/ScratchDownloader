using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;

namespace ScratchDownloader.Views.Control;

public class CsvDrawBackground : Avalonia.Controls.Control
{
    private double _animationTime = 0;
    private readonly DispatcherTimer _timer;
    private Size _lastBoundsSize;

    private int _currentIndex = 0;

    // --- CACHED DATA ---
    private List<Point>? _cachedCurrentWaypoints;
    private List<double>? _cachedCurrentDistances;
    private double _cachedCurrentTotalDistance;

    private List<Point>? _cachedNextWaypoints;

    // --- CONNECTOR FADE MEMORY ---
    private Point _lastConnectorStart;
    private Point _lastConnectorEnd;
    private bool _hasActiveConnectorFade = false;

    private const double SnakeSpeed = 12.0; // Pixels per frame (~60 FPS)
    private const double SnakeThickness = 10.0;
    private const double BaseSnakeOpacity = 0.55;

    // Transition timing
    private const double PathFadeDuration = 300.0;     // Distance units to fade out completed CSV path
    private const double ConnectorFadeDuration = 250.0; // Distance units to fade out connector line

    public event EventHandler? AnimationCompleted;

    public static readonly StyledProperty<IBrush?> AccentBrushProperty =
        AvaloniaProperty.Register<CsvDrawBackground, IBrush?>(nameof(AccentBrush));

    public static readonly StyledProperty<IList<string>?> CsvPathsProperty =
        AvaloniaProperty.Register<CsvDrawBackground, IList<string>?>(nameof(CsvPaths));

    public IBrush? AccentBrush
    {
        get => GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public IList<string>? CsvPaths
    {
        get => GetValue(CsvPathsProperty);
        set => SetValue(CsvPathsProperty, value);
    }

    static CsvDrawBackground()
    {
        CsvPathsProperty.Changed.AddClassHandler<CsvDrawBackground>((x, e) => x.OnCsvPathsChanged());
    }

    public CsvDrawBackground()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _timer.Tick += (s, e) =>
        {
            _animationTime += SnakeSpeed;
            InvalidateVisual();
        };
    }

    private void OnCsvPathsChanged()
    {
        _currentIndex = 0;
        _hasActiveConnectorFade = false;
        InvalidateCachedPath();
        ResetAnimation();
    }

    public void ResetAnimation()
    {
        _animationTime = 0;
        InvalidateVisual();
    }

    private void InvalidateCachedPath()
    {
        _cachedCurrentWaypoints = null;
        _cachedNextWaypoints = null;
        _cachedCurrentDistances = null;
        _cachedCurrentTotalDistance = 0;
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

    protected override void OnSizeChanged(Avalonia.Controls.SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        
        var bounds = Bounds;
        if (Math.Abs(_lastBoundsSize.Width - bounds.Width) > 5 ||
            Math.Abs(_lastBoundsSize.Height - bounds.Height) > 5)
        {
            _lastBoundsSize = bounds.Size;
            _hasActiveConnectorFade = false;
            InvalidateCachedPath();
            ResetAnimation();
        }
    }

    private void EnsurePathCached(Size bounds)
    {
        if (_cachedCurrentWaypoints != null || bounds.Width <= 0 || bounds.Height <= 0) return;

        var paths = CsvPaths;
        if (paths == null || paths.Count == 0) return;

        _currentIndex %= paths.Count;
        int nextIndex = (_currentIndex + 1) % paths.Count;

        string currentCsv = paths[_currentIndex];
        string nextCsv = paths[nextIndex];

        var currentWaypoints = ParseCsvPath(currentCsv, bounds.Width, bounds.Height);
        if (currentWaypoints == null || currentWaypoints.Count < 2)
        {
            _cachedCurrentWaypoints = null;
            _cachedNextWaypoints = null;
            _cachedCurrentDistances = null;
            _cachedCurrentTotalDistance = 0;
            return;
        }

        _cachedCurrentWaypoints = currentWaypoints;
        _cachedCurrentDistances = new List<double>(_cachedCurrentWaypoints.Count - 1);
        _cachedCurrentTotalDistance = 0;

        for (int i = 0; i < _cachedCurrentWaypoints.Count - 1; i++)
        {
            double dist = Distance(_cachedCurrentWaypoints[i], _cachedCurrentWaypoints[i + 1]);
            _cachedCurrentDistances.Add(dist);
            _cachedCurrentTotalDistance += dist;
        }

        _cachedNextWaypoints = ParseCsvPath(nextCsv, bounds.Width, bounds.Height);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        EnsurePathCached(bounds.Size);

        if (_cachedCurrentWaypoints == null || _cachedCurrentDistances == null || _cachedCurrentWaypoints.Count < 2) return;

        Point endPoint = _cachedCurrentWaypoints[^1];
        Point nextStartPoint = (_cachedNextWaypoints != null && _cachedNextWaypoints.Count > 0) 
            ? _cachedNextWaypoints[0] 
            : _cachedCurrentWaypoints[0];

        double connectorLength = Distance(endPoint, nextStartPoint);

        double t1_pathDone = _cachedCurrentTotalDistance;
        double t2_connectorDone = t1_pathDone + connectorLength;

        // --- CYCLE ADVANCEMENT ---
        if (_animationTime >= t2_connectorDone)
        {
            AnimationCompleted?.Invoke(this, EventArgs.Empty);

            // Store exact endpoints of connector line before switching indices
            _lastConnectorStart = endPoint;
            _lastConnectorEnd = nextStartPoint;
            _hasActiveConnectorFade = true;

            _currentIndex++;
            InvalidateCachedPath();

            _animationTime -= t2_connectorDone;

            EnsurePathCached(bounds.Size);
            if (_cachedCurrentWaypoints == null) return;
        }

        var baseBrush = AccentBrush as ISolidColorBrush ?? Brushes.DodgerBlue;

        // --- 1. CURRENT CSV PATH ---
        if (_animationTime <= t1_pathDone)
        {
            DrawSubPath(context, _cachedCurrentWaypoints, _cachedCurrentDistances, 0, _animationTime, baseBrush.Color, BaseSnakeOpacity);
        }
        else if (_animationTime <= t1_pathDone + PathFadeDuration)
        {
            double fadeProgress = 1.0 - ((_animationTime - t1_pathDone) / PathFadeDuration);
            DrawSubPath(context, _cachedCurrentWaypoints, _cachedCurrentDistances, 0, _cachedCurrentTotalDistance, baseBrush.Color, BaseSnakeOpacity * fadeProgress);
        }

        // --- 2. CONNECTOR LINE (CURRENT CYCLE) ---
        if (_animationTime > t1_pathDone && connectorLength > 0.1)
        {
            double lineDrawTime = _animationTime - t1_pathDone;
            double progress = Math.Clamp(lineDrawTime / connectorLength, 0.0, 1.0);

            Point currentLineEnd = Interpolate(endPoint, nextStartPoint, progress);
            DrawSingleLine(context, endPoint, currentLineEnd, baseBrush.Color, BaseSnakeOpacity);
        }

        // --- 3. CONNECTOR FADE OUT (PREVIOUS CYCLE) ---
        if (_hasActiveConnectorFade)
        {
            if (_animationTime < ConnectorFadeDuration)
            {
                double fadeProgress = 1.0 - (_animationTime / ConnectorFadeDuration);
                DrawSingleLine(context, _lastConnectorStart, _lastConnectorEnd, baseBrush.Color, BaseSnakeOpacity * fadeProgress);
            }
            else
            {
                _hasActiveConnectorFade = false; // Fade completed
            }
        }
    }

    private static void DrawSubPath(
        DrawingContext context,
        List<Point> waypoints,
        List<double> distances,
        double startDist,
        double endDist,
        Color color,
        double opacity)
    {
        if (endDist <= startDist || opacity <= 0) return;

        var subPath = new List<Point>();
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
                    subPath.Add(Interpolate(waypoints[i], waypoints[i + 1], tStart));
                    started = true;
                }

                if (segEnd >= endDist)
                {
                    double tEnd = Math.Clamp((endDist - segStart) / segLength, 0, 1);
                    subPath.Add(Interpolate(waypoints[i], waypoints[i + 1], tEnd));
                    break;
                }

                subPath.Add(waypoints[i + 1]);
            }

            currentDist += segLength;
        }

        if (subPath.Count < 2) return;

        var geometry = new StreamGeometry();
        using (var geoContext = geometry.Open())
        {
            geoContext.BeginFigure(subPath[0], false);
            for (int i = 1; i < subPath.Count; i++)
            {
                geoContext.LineTo(subPath[i]);
            }
            geoContext.EndFigure(false);
        }

        var pen = new Pen(new SolidColorBrush(color, opacity), SnakeThickness, lineJoin: PenLineJoin.Round, lineCap: PenLineCap.Round);
        context.DrawGeometry(null, pen, geometry);
    }

    private static void DrawSingleLine(DrawingContext context, Point p1, Point p2, Color color, double opacity)
    {
        if (opacity <= 0 || Distance(p1, p2) < 0.1) return;

        var pen = new Pen(new SolidColorBrush(color, opacity), SnakeThickness, lineJoin: PenLineJoin.Round, lineCap: PenLineCap.Round);
        context.DrawLine(pen, p1, p2);
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

        var points = new List<Point>(gridMap.Count);
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