using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ScratchDownloader.Converter
{
    public class IndexToGradientBrushConverter : IValueConverter
    {
        // Define total palette steps or pass via ConverterParameter
        public int TotalSegments { get; set; } = 8; 

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int index = 0;

            if (value is int i)
            {
                index = i;
            }

            // Calculate Hue shift (e.g., from 180° Cyan to 280° Purple, or 0° to 360°)
            // Adjust startHue and range to change the color palette:
            double startHue = 200; // Blue/Cyan region
            double hueRange = 120; // Extends through Teal/Green/Yellow

            double fraction = (double)(index % TotalSegments) / Math.Max(1, TotalSegments - 1);
            double hue = (startHue + (fraction * hueRange)) % 360;

            Color color = HslToRgb(hue, 0.75, 0.50);
            return new SolidColorBrush(color);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static Color HslToRgb(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c / 2;

            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }

            return Color.FromRgb(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255));
        }
    }
}