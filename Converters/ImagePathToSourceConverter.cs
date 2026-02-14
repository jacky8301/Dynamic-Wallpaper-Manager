using Serilog;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using WallpaperEngine.Services;

namespace WallpaperEngine.Converters {
    public class ImagePathToSourceConverter : IValueConverter {
        static string _defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview.jpg");
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try {
                string? imagePath = value as string;
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) {
                    Log.Warning("Image path is null, empty, or does not exist: {ImagePath}. Using default image.", value);
                    return GetDefaultImage();
                }
                if (ImageCache._cache.TryGetValue(imagePath, out var cachedImage)) {
                    return cachedImage;
                }
                // 磁盘缓存命中 → 存入内存缓存 → 返回
                var diskCached = ThumbnailDiskCache.TryLoad(imagePath);
                if (diskCached != null) {
                    ImageCache._cache[imagePath] = diskCached;
                    return diskCached;
                }
                var bitmap = ImageLoader.LoadImage(imagePath);
                ImageCache._cache[imagePath] = bitmap;
                // 异步写入磁盘缓存（不阻塞返回）
                if (bitmap != null) {
                    var bitmapToCache = bitmap;
                    Task.Run(() => ThumbnailDiskCache.Save(imagePath, bitmapToCache));
                }
                return bitmap;
            } catch (Exception ex) {
                Log.Fatal(ex, "Failed to load image from path: {ImagePath}", value);
                return GetDefaultImage();
            }
        }

        private static object GetDefaultImage()
        {
            if (ImageCache._cache.TryGetValue(_defaultPath, out var cachedDefault)) {
                return cachedDefault;
            }
            var defaulBitmap = ImageLoader.LoadImage(_defaultPath);
            ImageCache._cache[_defaultPath] = defaulBitmap;
            return defaulBitmap;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
