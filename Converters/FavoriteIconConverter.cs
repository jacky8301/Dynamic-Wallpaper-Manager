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
            // 处理可能的DependencyProperty.UnsetValue和NamedObject（DisconnectedItem）
            bool isFavorite = false;
            bool isMouseOver = false;

            if (values.Length >= 1)
            {
                object value0 = values[0];
                if (value0 is bool favorite)
                    isFavorite = favorite;
                else if (value0 == null || value0 == System.Windows.DependencyProperty.UnsetValue)
                {
                    // 忽略null和UnsetValue，保持false
                }
                else if (value0.GetType().Name == "NamedObject")
                {
                    // 处理WPF内部的DisconnectedItem，视为未设置
                }
                else
                {
                    // 尝试转换
                    bool.TryParse(value0.ToString(), out isFavorite);
                }
            }

            if (values.Length >= 2)
            {
                object value1 = values[1];
                if (value1 is bool mouseOver)
                    isMouseOver = mouseOver;
                else if (value1 == null || value1 == System.Windows.DependencyProperty.UnsetValue)
                {
                    // 忽略null和UnsetValue
                }
                else if (value1.GetType().Name == "NamedObject")
                {
                    // 处理DisconnectedItem
                }
                else
                {
                    bool.TryParse(value1.ToString(), out isMouseOver);
                }
            }

            // 根据 IsFavorite 状态返回不同的图标种类
            if (isFavorite) {
                return PackIconKind.Heart; // 实心心形表示已收藏
            }
            return PackIconKind.HeartOutline; // 默认空心心形（包括鼠标悬停状态）
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}