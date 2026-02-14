using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;
using WallpaperEngine.Services;
using System.Threading.Tasks;

namespace WallpaperEngine.Views {
    public partial class PreviewWindow : Window {
        private readonly WallpaperItem _wallpaper;
        private readonly Window _parentWindow;
        private DispatcherTimer _progressTimer;
        private bool _isDraggingProgress;
        private bool _isUpdatingProgress;

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
            if (e.ChangedButton == MouseButton.Left) {
                if (WindowState == WindowState.Maximized) {
                    WindowState = WindowState.Normal;
                }
                DragMove();
            }
        }
        private async Task LoadPreview()
        {
            if (_wallpaper == null) {
                ShowUnsupportedPreview();
                return;
            }

            try {
                switch (_wallpaper.Project.Type?.ToLower()) {
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
            } catch (Exception ex) {
                await MaterialDialogService.ShowErrorAsync($"加载预览失败: {ex.Message}", "错误");                   
                ShowUnsupportedPreview();
            }
        }

        private void ShowImagePreview()
        {
            try {
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
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"图片预览错误: {ex.Message}");
                ShowUnsupportedPreview();
            }
        }
        private void ShowVideoPreview()
        {
            try {
                VideoPreviewGrid.Visibility = Visibility.Visible;
                ImagePreviewGrid.Visibility = Visibility.Collapsed;
                UnsupportedText.Visibility = Visibility.Collapsed;

                PreviewVideo.Source = new Uri(_wallpaper.ContentPath);
                PreviewVideo.Volume = VolumeSlider.Value;
                PreviewVideo.LoadedBehavior = MediaState.Manual;
                PreviewVideo.MediaOpened += PreviewVideo_MediaOpened;
                PreviewVideo.Play();
                PlayPauseButton.Content = "⏸";

                _progressTimer = new DispatcherTimer {
                    Interval = TimeSpan.FromMilliseconds(250)
                };
                _progressTimer.Tick += ProgressTimer_Tick;
                _progressTimer.Start();
            } catch (Exception ex) {
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

            if (PlayPauseButton.Content.ToString() == "▶") {
                PreviewVideo.Play();
                PlayPauseButton.Content = "⏸";
                return;
            } else if (PlayPauseButton.Content.ToString() == "⏸") {
                PreviewVideo.Pause();
                PlayPauseButton.Content = "▶";
                return;
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            PreviewVideo.Stop();
            PlayPauseButton.Content = "▶";
            ProgressSlider.Value = 0;
            CurrentTimeText.Text = "00:00";
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PreviewVideo != null) {
                PreviewVideo.Volume = VolumeSlider.Value;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = _parentWindow.DataContext as MainViewModel;
            vm.ApplyWallpaperCommand.Execute(_wallpaper);
        }

        private void PreviewVideo_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (PreviewVideo.NaturalDuration.HasTimeSpan) {
                TotalTimeText.Text = FormatTime(PreviewVideo.NaturalDuration.TimeSpan);
            }
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (_isDraggingProgress || !PreviewVideo.NaturalDuration.HasTimeSpan) return;

            var total = PreviewVideo.NaturalDuration.TimeSpan.TotalSeconds;
            if (total > 0) {
                _isUpdatingProgress = true;
                ProgressSlider.Value = PreviewVideo.Position.TotalSeconds / total;
                _isUpdatingProgress = false;
                CurrentTimeText.Text = FormatTime(PreviewVideo.Position);
            }
        }

        private void ProgressSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            _isDraggingProgress = true;
        }

        private void ProgressSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            SeekToSliderPosition();
            _isDraggingProgress = false;
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingProgress || _isUpdatingProgress) return;
            if (_progressTimer == null) return;

            // 点击进度条时直接 seek
            SeekToSliderPosition();
        }

        private void SeekToSliderPosition()
        {
            if (PreviewVideo.NaturalDuration.HasTimeSpan) {
                var total = PreviewVideo.NaturalDuration.TimeSpan.TotalSeconds;
                PreviewVideo.Position = TimeSpan.FromSeconds(ProgressSlider.Value * total);
            }
        }

        private static string FormatTime(TimeSpan time)
        {
            return time.Hours > 0
                ? time.ToString(@"h\:mm\:ss")
                : time.ToString(@"mm\:ss");
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_progressTimer != null) {
                _progressTimer.Stop();
                _progressTimer = null;
            }

            if (PreviewVideo != null) {
                PreviewVideo.Stop();
                PreviewVideo.Source = null;
                PreviewVideo.Close();
            }

            if (PreviewImage is BitmapImage bitmapImage) {
                bitmapImage.StreamSource?.Dispose();
            }

            base.OnClosed(e);
        }
    }
}