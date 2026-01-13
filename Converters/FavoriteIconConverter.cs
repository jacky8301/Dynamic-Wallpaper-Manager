using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WallpaperEngine.Converters
{
   public class FavoriteIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool isFavorite && values[1] is bool isMouseOver)
            {
                // 可根据 IsFavorite 和 IsMouseOver 状态返回不同的图标
                if (isFavorite)
                {
                    return "★"; // 实心星星表示已收藏
                }
                else if (isMouseOver)
                {
                    return "☆"; // 空心星星表示悬停时可收藏
                }
            }
            return "☆"; // 默认空心星星
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}