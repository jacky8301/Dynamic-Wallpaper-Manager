using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Serilog;

namespace WallpaperEngine.Converters
{
    /// <summary>
    /// 将壁纸对象和分类ID组合成数组，用于传递给AddToSpecificCategory命令
    /// </summary>
    public class AddToCategoryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return new object?[2] { null, null };

            object? wallpaperObj = values[0];
            object? categoryIdObj = values[1];

            if (wallpaperObj == DependencyProperty.UnsetValue)
                wallpaperObj = null;

            if (categoryIdObj == DependencyProperty.UnsetValue)
                categoryIdObj = null;

            return new object?[] { wallpaperObj, categoryIdObj };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
