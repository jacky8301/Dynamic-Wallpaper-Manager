using Serilog;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using WallpaperEngine.Services;

namespace WallpaperEngine.Converters {
    public class ImagePathToSourceConverter : IValueConverter {
        static string _defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview.jpg");
        private static readonly ConcurrentDictionary<string, BitmapImage> _cache =
            new ConcurrentDictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try {
                string imagePath = value as string;
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) {
                    Log.Warning("Image path is null, empty, or does not exist: {ImagePath}. Using default image.", value);
                    return GetDefaultImage();
                }
                if (_cache.TryGetValue(imagePath, out var cachedImage)) {
                    return cachedImage;
                }
                var bitmap = ImageLoader.LoadImage(imagePath);
                _cache[imagePath] = bitmap;
                return bitmap;
            } catch (Exception ex) {
                Log.Fatal(ex, "Failed to load image from path: {ImagePath}", value);
                return GetDefaultImage();
            }
        }

        private static object GetDefaultImage()
        {
            if (_cache.TryGetValue(_defaultPath, out var cachedDefault)) {
                return cachedDefault;
            }
            var defaulBitmap = ImageLoader.LoadImage(_defaultPath);
            _cache[_defaultPath] = defaulBitmap;
            return defaulBitmap;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
