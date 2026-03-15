using Serilog;
using System.IO;
using System.Windows.Media.Imaging;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 图片加载静态工具类，提供多种方式加载 BitmapImage 且不锁定源文件
    /// </summary>
    public static class ImageLoader {
        /// <summary>
        /// 加载图片但不锁定文件，加载失败时返回 null
        /// </summary>
        /// <param name="filePath">图片文件的绝对路径</param>
        /// <returns>加载成功返回 BitmapImage，否则返回 null</returns>
        public static BitmapImage LoadImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) { return null; }
            try {
                return LoadImageWithUri(filePath);
            } catch (Exception ex) {
                Log.Warning(ex, "加载图片失败 {FilePath}", filePath);
                return null;
            }
        }
        /// <summary>
        /// 通过 URI 方式加载图片，设置解码宽度为 200 像素以节省���存，加载后冻结以释放文件锁
        /// </summary>
        /// <param name="filePath">图片文件的绝对路径</param>
        /// <returns>加载成功返回冻结的 BitmapImage，文件不存在时返回 null</returns>
        public static BitmapImage LoadImageWithUri(string filePath)
        {
            Log.Debug("LoadImageWithUri Start, filePath: {FilePath}", filePath);
            if (!File.Exists(filePath)) { return null; }
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;  // 加载后不锁定文件
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;  // 忽略缓存
            bitmap.DecodePixelWidth = 200;  // 可选：根据需要调整解码像素宽度以节省内存
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();  // 冻结可确保文件被释放
            Log.Debug("LoadImageWithUri finish, filePath: {FilePath}", filePath);
            return bitmap;
        }
    }
}
