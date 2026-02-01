using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace WallpaperEngine.Converters {
    public class DebugConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 调试器会在此处中断，你可以检查value的值
            Debugger.Break();
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }
    }
}
