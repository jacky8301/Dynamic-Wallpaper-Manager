using System.Globalization;
using System.Windows.Data;

namespace WallpaperEngine.Converters {
    /// <summary>
    /// 将布尔值转换为对应的画刷颜色，支持自定义 TrueBrush 和 FalseBrush
    /// </summary>
    public class BooleanToColorConverter : IValueConverter {
        public System.Windows.Media.Brush TrueBrush { get; set; } = System.Windows.Media.Brushes.Green;
        public System.Windows.Media.Brush FalseBrush { get; set; } = System.Windows.Media.Brushes.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueBrush : FalseBrush;
            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}