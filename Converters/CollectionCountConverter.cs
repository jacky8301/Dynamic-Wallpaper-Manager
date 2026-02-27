using System.Globalization;
using System.Windows.Data;

namespace WallpaperEngine.Converters
{
    /// <summary>
    /// 将合集数量转换为显示文本
    /// </summary>
    public class CollectionCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? "📂 添加到合集 (暂无合集)" : $"📂 添加到合集 ({count}个合集)";
            }
            return "📂 添加到合集";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
