using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WallpaperEngine.Services {
    internal class ImageCache {
        public static readonly ConcurrentDictionary<string, BitmapImage> _cache =
            new ConcurrentDictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);

            public static BitmapImage GetOrAdd(string path, Func<string, BitmapImage> valueFactory)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }
            return _cache.GetOrAdd(path, valueFactory);
        }

    }
}
