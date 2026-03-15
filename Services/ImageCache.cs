using System.Collections.Concurrent;
using System.Windows.Media.Imaging;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 线程安全的内存图片缓存，使用 ConcurrentDictionary 存储已加载的 BitmapImage，带 LRU 淘汰
    /// </summary>
    internal class ImageCache {
        private static readonly ConcurrentDictionary<string, BitmapImage> _cache =
            new ConcurrentDictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, long> _accessOrder =
            new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        private static long _accessCounter = 0;
        private const int MaxCacheSize = 500;

        /// <summary>
        /// 尝试从缓存获取图片
        /// </summary>
        public static bool TryGetValue(string path, out BitmapImage image)
        {
            if (_cache.TryGetValue(path, out image)) {
                _accessOrder[path] = Interlocked.Increment(ref _accessCounter);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置缓存项
        /// </summary>
        public static void Set(string path, BitmapImage image)
        {
            _cache[path] = image;
            _accessOrder[path] = Interlocked.Increment(ref _accessCounter);
            if (_cache.Count > MaxCacheSize) {
                Evict();
            }
        }

        /// <summary>
        /// 根据路径获取缓存的图片，若不存在则通过工厂方法创建并缓存
        /// </summary>
        public static BitmapImage GetOrAdd(string path, Func<string, BitmapImage> valueFactory)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            if (_cache.TryGetValue(path, out var existing)) {
                _accessOrder[path] = Interlocked.Increment(ref _accessCounter);
                return existing;
            }

            var image = valueFactory(path);
            _cache[path] = image;
            _accessOrder[path] = Interlocked.Increment(ref _accessCounter);

            if (_cache.Count > MaxCacheSize) {
                Evict();
            }

            return image;
        }

        private static void Evict()
        {
            var toRemove = _accessOrder
                .OrderBy(kv => kv.Value)
                .Take(_cache.Count - MaxCacheSize + MaxCacheSize / 4)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in toRemove) {
                _cache.TryRemove(key, out _);
                _accessOrder.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
            _accessOrder.Clear();
        }
    }
}
