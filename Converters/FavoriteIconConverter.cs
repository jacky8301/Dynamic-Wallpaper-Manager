using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace WallpaperEngine.Converters {
    /// <summary>
    /// 多值转换器，根据收藏状态返回对应的Material Design心形图标种类（实心/空心）
    /// </summary>
    public class FavoriteIconConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool isFavorite && values[1] is bool isMouseOver) {
                // 根据 IsFavorite 状态返回不同的图标种类
                if (isFavorite) {
                    return PackIconKind.Heart; // 实心心形表示已收藏
                }
            }
            return PackIconKind.HeartOutline; // 默认空心心形（包括鼠标悬停状态）
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}