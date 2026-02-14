using Serilog;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 磁盘缩略图缓存，使用 SHA256 哈希文件名存储缩略图，支持过期检测
    /// </summary>
    public static class ThumbnailDiskCache {
        private static readonly string _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicWallpaperManager", "ThumbnailCache");

        static ThumbnailDiskCache()
        {
            Directory.CreateDirectory(_cacheDir);
        }

        /// <summary>
        /// 尝试从磁盘缓存加载缩略图，若缓存不存在或已过期则返回 null
        /// </summary>
        /// <param name="originalPath">原始图片的文件路径</param>
        /// <returns>缓存命中返回冻结的 BitmapImage，否则返回 null</returns>
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

        /// <summary>
        /// 将缩略图以 JPEG 格式保存到磁盘缓存，使用临时文件确保写入原子性
        /// </summary>
        /// <param name="originalPath">原始图片的文件路径，用于生成缓存键</param>
        /// <param name="bitmap">要缓存的缩略图</param>
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

        /// <summary>
        /// 根据原始文件路径生成 SHA256 哈希缓存文件路径
        /// </summary>
        /// <param name="originalPath">原始图片的文件路径</param>
        /// <returns>缓存文件的完整路径</returns>
        private static string GetCachePath(string originalPath)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(originalPath.ToLowerInvariant()));
            string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return Path.Combine(_cacheDir, hex + ".jpg");
        }
    }
}
