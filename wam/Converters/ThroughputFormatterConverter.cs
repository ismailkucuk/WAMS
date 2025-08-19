using System;
using System.Globalization;
using System.Windows.Data;

namespace wam.Converters
{
    // Formats Mbps to human-friendly string. If < 1 Mbps, shows Kbps; if < 0.1 Kbps, shows bps.
    public class ThroughputFormatterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double mbps = 0;
                if (value is double d) mbps = d;
                else if (value is float f) mbps = f;
                else if (value is int i) mbps = i;

                if (mbps >= 1.0)
                    return string.Format(culture, "{0:F2} Mbps", mbps);

                double kbps = mbps * 1000.0;
                if (kbps >= 1.0)
                    return string.Format(culture, "{0:F1} Kbps", kbps);

                double bps = kbps * 1000.0;
                return string.Format(culture, "{0:F0} bps", bps);
            }
            catch
            {
                return "0 bps";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}



