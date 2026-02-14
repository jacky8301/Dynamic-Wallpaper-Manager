using System.Collections.Concurrent;
using System.Windows.Media.Imaging;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 线程安全的内存图片缓存，使用 ConcurrentDictionary 存储已加载的 BitmapImage
    /// </summary>
    internal class ImageCache {
        public static readonly ConcurrentDictionary<string, BitmapImage> _cache =
            new ConcurrentDictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// 根据路径获取缓存的图片，若不存在则通过工厂方法创建并缓存
            /// </summary>
            /// <param name="path">图片文件路径（不区分大小写）</param>
            /// <param name="valueFactory">缓存未命中时用于创建 BitmapImage 的工厂方法</param>
            /// <returns>缓存或新创建的 BitmapImage</returns>
            public static BitmapImage GetOrAdd(string path, Func<string, BitmapImage> valueFactory)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }
            return _cache.GetOrAdd(path, valueFactory);
        }

    }
}
