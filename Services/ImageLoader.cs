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
                Log.Warning($"加载图片失败 {filePath}: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 通过 URI 方式加载图片，设置解码宽度为 200 像素以节省内存，加载后冻结以释放文件锁
        /// </summary>
        /// <param name="filePath">图片文件的绝对路径</param>
        /// <returns>加载成功返回冻结的 BitmapImage，文件不存在时返回 null</returns>
        public static BitmapImage LoadImageWithUri(string filePath)
        {
            Log.Debug("LoadImageWithUri Start, filePath:" + filePath);
            if (!File.Exists(filePath)) { return null; }
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;  // 加载后不锁定文件
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;  // 忽略缓存
            bitmap.DecodePixelWidth = 200;  // 可选：根据需要调整解码像素宽度以节省内存
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();  // 冻结可确保文件被释放
            Log.Debug("LoadImageWithUri finish, filePath:" + filePath);
            return bitmap;
        }
        /// <summary>
        /// 通过字节数组方式加载图片，先读取全部字节再创建 BitmapImage，不锁定文件
        /// </summary>
        /// <param name="filePath">图片文件的绝对路径</param>
        /// <returns>加载成功返回 BitmapImage，否则返回 null</returns>
        public static BitmapImage LoadImagev2(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            try {
                byte[] imageData = File.ReadAllBytes(filePath);
                return LoadImageToBytes(imageData);
            } catch (Exception ex) {
                Log.Warning($"加载图片失败 {filePath}: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 从字节数组加载图片并返回冻结的 BitmapImage
        /// </summary>
        /// <param name="imageData">图片的字节数组数据</param>
        /// <returns>加载成功返回 BitmapImage，数据为空时返回 null</returns>
        public static BitmapImage LoadImageToBytes(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) { return null; }
            using (MemoryStream stream = new MemoryStream(imageData)) {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }
    }
}
