using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views
{
    public partial class PreviewWindow : Window
    {
        private readonly WallpaperItem _wallpaper;
        private readonly Window _parentWindow;

        public string WallpaperTitle => _wallpaper?.Project.Title ?? "壁纸预览";
        public ImageSource PreviewImage { get; private set; }

        public PreviewWindow(WallpaperItem wallpaper)
        {
            _wallpaper = wallpaper;
            InitializeComponent();
            _parentWindow = System.Windows.Application.Current.MainWindow;
            LoadPreview();
        }
        // 窗口按钮事件处理
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        // 允许通过拖动标题栏移动窗口
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                }
                DragMove();
            }
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
                        break;
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
                PreviewVideo.LoadedBehavior = MediaState.Manual;
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

            if (PlayPauseButton.Content.ToString() == "▶")
            {
                PreviewVideo.Play();
                PlayPauseButton.Content = "⏸";
                return;
            }
            else if (PlayPauseButton.Content.ToString() == "⏸")
            {
                PreviewVideo.Pause();
                PlayPauseButton.Content = "▶";
                return;
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
            var vm = _parentWindow.DataContext as MainViewModel;
            vm.ApplyWallpaperCommand.Execute(_wallpaper);
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