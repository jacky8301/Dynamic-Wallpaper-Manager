using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WallpaperEngine.Models;

namespace WallpaperEngine.Views
{
    public partial class PreviewWindow : Window
    {
        private readonly WallpaperItem _wallpaper;

        public string WallpaperTitle => _wallpaper?.Project.Title ?? "壁纸预览";
        public ImageSource PreviewImage { get; private set; }

        public PreviewWindow(WallpaperItem wallpaper)
        {
            _wallpaper = wallpaper;
            InitializeComponent();
            DataContext = this;
            LoadPreview();
        }

        private void LoadPreview()
        {
            if (_wallpaper == null)
            {
                ShowUnsupportedPreview();
                return;
            }

            try
            {
                switch (_wallpaper.Project.Type?.ToLower())
                {
                    case "video":
                        ShowVideoPreview();
                        break;
                    case "scene":
                    case "web":
                    case "application":
                        ShowUnsupportedPreview();
                        break;
                    default:
                        ShowImagePreview();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载预览失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ShowUnsupportedPreview();
            }
        }

        private void ShowImagePreview()
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_wallpaper.PreviewImagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 800;
                bitmap.EndInit();

                PreviewImage = bitmap;
                ImagePreviewGrid.Visibility = Visibility.Visible;
                VideoPreviewGrid.Visibility = Visibility.Collapsed;
                UnsupportedText.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"图片预览错误: {ex.Message}");
                ShowUnsupportedPreview();
            }
        }

        private void ShowVideoPreview()
        {
            try
            {
                VideoPreviewGrid.Visibility = Visibility.Visible;
                ImagePreviewGrid.Visibility = Visibility.Collapsed;
                UnsupportedText.Visibility = Visibility.Collapsed;

                PreviewVideo.Source = new Uri(_wallpaper.ContentPath);
                PreviewVideo.Volume = VolumeSlider.Value;
                PreviewVideo.Play();
                PlayPauseButton.Content = "⏸";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"视频预览错误: {ex.Message}");
                ShowUnsupportedPreview();
            }
        }

        private void ShowUnsupportedPreview()
        {
            ImagePreviewGrid.Visibility = Visibility.Collapsed;
            VideoPreviewGrid.Visibility = Visibility.Collapsed;
            UnsupportedText.Visibility = Visibility.Visible;
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewVideo.Source == null) return;

            if (PreviewVideo.CanPause)
            {
                PreviewVideo.Pause();
                PlayPauseButton.Content = "▶";
            }
            else
            {
                PreviewVideo.Play();
                PlayPauseButton.Content = "⏸";
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            PreviewVideo.Stop();
            PlayPauseButton.Content = "▶";
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PreviewVideo != null)
            {
                PreviewVideo.Volume = VolumeSlider.Value;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.MessageBox.Show($"已应用壁纸: {_wallpaper.Project.Title}",
                    "应用成功", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"应用壁纸失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (PreviewVideo != null)
            {
                PreviewVideo.Stop();
                PreviewVideo.Source = null;
                PreviewVideo.Close();
            }

            if (PreviewImage is BitmapImage bitmapImage)
            {
                bitmapImage.StreamSource?.Dispose();
            }

            base.OnClosed(e);
        }
    }
}