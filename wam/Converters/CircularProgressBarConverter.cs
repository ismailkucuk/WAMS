using System;
using System.Globalization;
using System.Windows.Data;

namespace wam.Converters
{
    public class CircularProgressBarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Değeri double'a çevir
                double progress = 0;
                if (value is int intValue)
                    progress = intValue;
                else if (value is double doubleValue)
                    progress = doubleValue;
                else if (value is float floatValue)
                    progress = floatValue;
                else
                    return 283; // Varsayılan değer

                // Progress değerini 0-100 arasında sınırla
                progress = Math.Max(0, Math.Min(100, progress));

                // Yarıçap 45px (çap 90px)
                double radius = 45;
                double circumference = 2 * Math.PI * radius; // ≈ 283

                // StrokeDashOffset hesaplama: 
                // 100% = 0 offset (tam dolu)
                // 0% = circumference offset (tam boş)
                double offset = circumference * (1 - progress / 100.0);

                return Math.Max(0, offset);
            }
            catch
            {
                return 283; // Hata durumunda tam boş
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 