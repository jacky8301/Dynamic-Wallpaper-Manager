using System.Globalization;
using System.IO;
using System.Windows.Data;
using WallpaperEngine.Services;

namespace WallpaperEngine.Converters {
    public class ImagePathToSourceConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string imagePath = value as string;
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) {
                return null;
            }
            return ImageLoader.LoadImage(imagePath);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
