using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using WallpaperEngine.Services;

namespace WallpaperEngine.Converters {
    public class ImagePathToSourceConverter : IValueConverter {
        private static readonly ConcurrentDictionary<string, BitmapImage> _cache =
            new ConcurrentDictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string imagePath = value as string;
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) {
                return null;
            }
            if (_cache.TryGetValue(imagePath, out var cachedImage)) {
                return cachedImage;
            }
            var bitmap = ImageLoader.LoadImage(imagePath);
            _cache[imagePath] = bitmap;
            return bitmap;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
