using Serilog;
using System.IO;
using System.Windows.Media.Imaging;

namespace WallpaperEngine.Services {
    public static class ImageLoader {
        /// 加载图片但不锁定文件
        public static BitmapImage LoadImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) { return null; }
            try {
                return LoadImageWithUri(filePath);
            } catch {
                return null;
            }
        }
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
        /// 加载图片但不锁定文件
        public static BitmapImage LoadImagev2(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            try {
                byte[] imageData = File.ReadAllBytes(filePath);
                return LoadImageToBytes(imageData);
            } catch {
                return null;
            }
        }
        // 从字节数组加载图片
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
