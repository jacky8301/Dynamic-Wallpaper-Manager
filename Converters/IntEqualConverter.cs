using System.Globalization;
using System.Windows.Data;

namespace WallpaperEngine.Converters
{
    [ValueConversion(typeof(int), typeof(bool))]
    public class IntEqualConverter : IValueConverter {
        public int Target { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && i == Target;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Target;
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
