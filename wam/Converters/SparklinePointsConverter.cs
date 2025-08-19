using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace wam.Converters
{
    // Converts a sequence of doubles into a PointCollection suitable for Polyline.
    // Parameter format: "width,height" (e.g., "160,28")
    public class SparklinePointsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is not IEnumerable enumerable) return null;
                var vals = enumerable.Cast<object>()
                    .Select(v =>
                    {
                        if (v is double d) return d;
                        if (v is float f) return (double)f;
                        if (v is int i) return (double)i;
                        return 0d;
                    })
                    .ToList();

                if (vals.Count == 0) return null;

                double width = 160, height = 28;
                if (parameter is string p && p.Contains(','))
                {
                    var parts = p.Split(',');
                    double.TryParse(parts[0], out width);
                    double.TryParse(parts[1], out height);
                }

                double max = Math.Max(1e-6, vals.Max());
                int n = vals.Count;
                if (n == 1) n = 2; // avoid division by zero
                double stepX = width / (n - 1);

                var pc = new PointCollection();
                for (int i = 0; i < vals.Count; i++)
                {
                    double x = i * stepX;
                    double norm = max > 0 ? vals[i] / max : 0;
                    double y = height - (norm * height);
                    pc.Add(new System.Windows.Point(x, y));
                }
                return pc;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}



