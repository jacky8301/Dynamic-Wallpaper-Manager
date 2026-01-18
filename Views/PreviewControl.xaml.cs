using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WallpaperEngine.Models;

namespace WallpaperEngine.Views
{
    public partial class PreviewControl : System.Windows.Controls.UserControl
    {
        private WallpaperItem _currentWallpaper;
        private bool _isDragging = false;
        private System.Windows.Point _lastMousePosition;
        private double _totalScale = 1.0;
        private double _rotationAngle = 0;

        public PreviewControl()
        {
            InitializeComponent();
        }

        public void LoadPreview(WallpaperItem wallpaper)
        {
            _currentWallpaper = wallpaper;

            if (wallpaper?.Project == null) return;

            // 根据壁纸类型显示相应预览
            switch (wallpaper.Project.Type?.ToLower())
            {
                case "video":
                    ShowVideoPreview();
                    break;
                default:
                    ShowImagePreview();
                    break;
            }
        }

        private void ShowImagePreview()
        {
            ImagePreview.Visibility = Visibility.Visible;
            VideoPreview.Visibility = Visibility.Collapsed;
            ControlPanel.Visibility = Visibility.Visible;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_currentWallpaper.PreviewImagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 800; // 限制解码尺寸优化内存[1](@ref)
                bitmap.EndInit();

                aPreviewImage.Source = bitmap;
                FitToWindow();
            }
            catch
            {
                // 处理图片加载失败
            }
        }

        private void ShowVideoPreview()
        {
            VideoPreview.Visibility = Visibility.Visible;
            ImagePreview.Visibility = Visibility.Collapsed;
            ControlPanel.Visibility = Visibility.Visible;

            try
            {
                PreviewVideo.Source = new Uri(_currentWallpaper.ContentPath);
                PreviewVideo.Play();
            }
            catch
            {
                // 处理视频加载失败
            }
        }

        // 缩放、平移、旋转等控制方法[2](@ref)
        private void PreviewImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scale = e.Delta > 0 ? 1.1 : 0.9;
            _totalScale *= scale;
            _totalScale = Math.Max(0.1, Math.Min(10, _totalScale));
            ImageScale.ScaleX = ImageScale.ScaleY = _totalScale;
        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e) => RotateImage(-90);
        private void RotateRight_Click(object sender, RoutedEventArgs e) => RotateImage(90);

        private void RotateImage(double angle)
        {
            _rotationAngle += angle;
            ImageRotate.Angle = _rotationAngle;
        }

        private void FitToWindow_Click(object sender, RoutedEventArgs e) => FitToWindow();

        private void FitToWindow()
        {
            if (aPreviewImage.Source == null) return;

            var containerWidth = ActualWidth;
            var containerHeight = ActualHeight;
            var imageWidth = aPreviewImage.Source.Width;
            var imageHeight = aPreviewImage.Source.Height;

            var scale = Math.Min(containerWidth / imageWidth, containerHeight / imageHeight);
            _totalScale = scale;
            ImageScale.ScaleX = ImageScale.ScaleY = scale;

            // 重置位置和旋转
            ImageTranslate.X = ImageTranslate.Y = 0;
            ImageRotate.Angle = 0;
            _rotationAngle = 0;
        }

        private void ActualSize_Click(object sender, RoutedEventArgs e)
        {
            _totalScale = 1.0;
            ImageScale.ScaleX = ImageScale.ScaleY = 1.0;
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewVideo.Source == null) return;

            if (PreviewVideo.CanPause)
            {
                PreviewVideo.Pause();
                PlayPauseBtn.Content = "▶";
            }
            else
            {
                PreviewVideo.Play();
                PlayPauseBtn.Content = "⏸";
            }
        }
    }
}