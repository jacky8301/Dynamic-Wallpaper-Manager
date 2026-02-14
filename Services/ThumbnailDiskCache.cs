using Serilog;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace WallpaperEngine.Services {
    public static class ThumbnailDiskCache {
        private static readonly string _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicWallpaperManager", "ThumbnailCache");

        static ThumbnailDiskCache()
        {
            Directory.CreateDirectory(_cacheDir);
        }

        public static BitmapImage? TryLoad(string originalPath)
        {
            try {
                string cachePath = GetCachePath(originalPath);
                if (!File.Exists(cachePath)) return null;

                // 过期检测：原始文件更新则缓存失效
                if (File.GetLastWriteTimeUtc(originalPath) > File.GetLastWriteTimeUtc(cachePath))
                    return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = new Uri(cachePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            } catch (Exception ex) {
                Log.Debug(ex, "Failed to load disk cache for {Path}", originalPath);
                return null;
            }
        }

        public static void Save(string originalPath, BitmapImage bitmap)
        {
            try {
                string cachePath = GetCachePath(originalPath);
                string tempPath = cachePath + ".tmp";

                var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write)) {
                    encoder.Save(fs);
                }

                File.Move(tempPath, cachePath, overwrite: true);
            } catch (Exception ex) {
                Log.Debug(ex, "Failed to save disk cache for {Path}", originalPath);
            }
        }

        private static string GetCachePath(string originalPath)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(originalPath.ToLowerInvariant()));
            string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return Path.Combine(_cacheDir, hex + ".jpg");
        }
    }
}
