using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WallpaperEngine.Services
{
    public static class ImageLoader
    {
        /// <summary>
        /// 加载图片但不锁定文件
        /// </summary>
        public static BitmapImage LoadImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                //byte[] imageData = File.ReadAllBytes(filePath);
                return LoadImageWithUri(filePath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从字节数组加载图片
        /// </summary>
        public static BitmapImage LoadImageWithUri(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;  // 加载后不锁定文件
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;  // 忽略缓存
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();  // 冻结可确保文件被释放
            return bitmap;
            //using (MemoryStream stream = new MemoryStream(imageData))
            //{
            //    BitmapImage bitmap = new BitmapImage();
            //    bitmap.BeginInit();
            //    bitmap.CacheOption = BitmapCacheOption.OnLoad;
            //    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;  // 忽略缓存
            //    bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            //    bitmap.EndInit();
            //    bitmap.Freeze();
            //    return bitmap;
            //}
        }
    }
}
