using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WallpaperEngine.Converters {
    /// <summary>
    /// 布尔值取反后转换为可见性，true 时隐藏（Collapsed），false 时显示（Visible）
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseBooleanToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 将 bool 值取反
           if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 若需双向绑定，可在此实现反向逻辑；否则抛出异常
            throw new NotSupportedException();
        }
    }
}
