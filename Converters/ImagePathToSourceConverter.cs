using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WallpaperEngine.Converters
{
    public class ImagePathToSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && File.Exists(path))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad; // 关键：加载后释放文件
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // 忽略旧缓存
                                                                                // 可选：设置解码尺寸以优化性能
                    image.DecodePixelWidth = 200;
                    image.UriSource = new Uri(path, UriKind.Absolute);
                    image.EndInit();
                    image.Freeze(); // 建议冻结，使图像可跨线程访问
                    return image;
                }
                catch (Exception e)
                {
                    return DependencyProperty.UnsetValue;
                }
            }
            // 可以返回一个默认图片或null
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
