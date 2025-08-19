using System;
using System.Globalization;
using System.Windows.Data;

namespace wam.Pages
{
    public class CircularProgressBarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double percentage = 0;
                
                // Handle both int and double values
                if (value is int intVal)
                {
                    percentage = intVal;
                }
                else if (value is double doubleVal)
                {
                    percentage = doubleVal;
                }
                else if (value != null && double.TryParse(value.ToString(), out double parsedVal))
                {
                    percentage = parsedVal;
                }

                // Clamp percentage between 0 and 100
                percentage = Math.Max(0, Math.Min(100, percentage));

                // For 64px diameter ring with 4px stroke thickness
                // Radius = (64 - 4) / 2 = 30px
                double radius = 30;
                double circumference = 2 * Math.PI * radius; // ≈ 188.5 -> rounded to 201 for cleaner dash array
                
                // Calculate stroke dash offset
                double progressLength = (percentage / 100.0) * 201;
                double offset = 201 - progressLength;
                
                return offset;
            }
            catch
            {
                // Return default offset for full circumference (no progress)
                return 201;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 