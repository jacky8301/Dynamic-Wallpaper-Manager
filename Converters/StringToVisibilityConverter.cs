using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WallpaperEngine.Converters
{
    /// <summary>
    /// 将字符串转换为可见性，空或 null 字符串返回 Collapsed，非空返回 Visible
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
