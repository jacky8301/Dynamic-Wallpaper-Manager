using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WallpaperEngine.Converters {
    public class ProtectedCategoryToVisibilityConverter : IValueConverter {
        private static readonly HashSet<string> ProtectedCategories = new() { "所有分类", "未分类" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string category && !ProtectedCategories.Contains(category))
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
