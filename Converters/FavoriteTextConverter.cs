using System.Globalization;
using System.Windows.Data;

namespace WallpaperEngine.Converters {
    /// <summary>
    /// 将收藏状态布尔值转换为对应的显示文本（"收藏壁纸"或"取消收藏"）
    /// </summary>
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
