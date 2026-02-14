using System;
using System.Globalization;
using System.Windows.Data;

namespace WallpaperEngine.Converters {
    /// <summary>
    /// 将字符串值转换为布尔值，非空字符串返回 true，空或 null 返回 false
    /// </summary>
    public class MyMethodToBoolConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 这里可以调用你的方法，或直接写逻辑
            // 例如，假设你的方法是 CheckIfButtonShouldBeEnabled(string input)
            string input = value as string;
            return !string.IsNullOrEmpty(input);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
