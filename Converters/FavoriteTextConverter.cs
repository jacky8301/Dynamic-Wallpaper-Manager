using System.Globalization;
using System.Windows.Data;

namespace WallpaperEngine.Converters {
    public class FavoriteTextConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFavorite && isFavorite)
                return "💔 取消收藏";
            return "⭐️ 收藏壁纸";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
