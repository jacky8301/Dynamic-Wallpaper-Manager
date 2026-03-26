using CommunityToolkit.Mvvm.DependencyInjection;
using Serilog;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WallpaperEngine.Common;
using WallpaperEngine.ViewModels;
using WallpaperEngine.Models;

namespace WallpaperEngine.Views {
    /// <summary>
    /// 应用程序主窗口，包含加载遮罩层、系统托盘图标、标题栏拖拽及壁纸加载等功能
    /// </summary>
    public partial class MainWindow : System.Windows.Window {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetService<MainViewModel>();
            this.DataContext = ViewModel;
            CollectionViewPanel.DataContext = Ioc.Default.GetService<CollectionViewModel>();
            ViewModel.LoadWallpapersCompleted += (s, e) => {
                // 在壁纸加载完成后隐藏加载层
                Dispatcher.Invoke(() => HideLoadingOverlay());
            };

            // 切换分类时重置滚动条位置
            ViewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(MainViewModel.SelectedCategoryId))
                {
                    _wallpaperScrollViewer ??= WpfHelper.FindScrollViewer(WallpaperListBox);
                    _wallpaperScrollViewer?.ScrollToHome();
                }
            };

            // 手动添加ListBox事件处理器以避免XAML解析错误
            if (WallpaperListBox != null)
            {
                WallpaperListBox.PreviewMouseDown += ListBox_PreviewMouseDown;
                WallpaperListBox.PreviewMouseRightButtonDown += ListBox_PreviewMouseRightButtonDown;
                WallpaperListBox.SelectionChanged += WallpaperListBox_SelectionChanged;
            }

            // 添加窗口关闭事件处理，确保托盘图标被正确释放
            this.Closing += MainWindow_Closing;
        }

        // 窗口关闭时释放托盘图标资源
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            TrayIcon?.Dispose();
        }
        private MainViewModel ViewModel;
        private ScrollViewer? _wallpaperScrollViewer;

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
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized) {
                // 最大化时设置边距，防止内容溢出屏幕
                BorderThickness = new Thickness(8);
            } else {
                BorderThickness = new Thickness(0);
            }
        }

        private async void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try {
                // 从App类获取启动参数
                string[] args = Environment.GetCommandLineArgs();

                // 判断是否包含-autostart参数
                bool isAutoStart = args.Contains("-autostart");

                if (isAutoStart) {
                    this.Hide();
                    this.WindowState = WindowState.Minimized;
                }
                Log.Debug("LoadWallpapersAsync started");
                await ViewModel.LoadWallpapersAsync();
                Log.Debug("LoadWallpapersAsync finish");

                // 恢复最后应用的壁纸
                ViewModel.RestoreLastWallpaper();
            } catch (Exception ex) {
                Log.Error(ex, "mainWindow_Loaded 发生未处理异常");
            }
        }
        // 隐藏加载层并显示主内容
        private void HideLoadingOverlay()
        {
            // 创建一个淡出动画
            var fadeOutAnimation = new DoubleAnimation {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // 动画完成后的回调
            fadeOutAnimation.Completed += (s, args) =>
            {
                // 完全隐藏叠加层，并恢复主内容的交互能力
                LoadingOverlay.Visibility = Visibility.Collapsed;
                LoadingOverlay.IsHitTestVisible = false;
            };

            // 开始动画
            LoadingOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
        }
        // 双击托盘图标：显示窗口
        private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowMainWindow();
        }
        // 右键菜单项：显示窗口
        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            ShowMainWindow();
        }
        // 公共的显示窗口方法
        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate(); // 将窗口提到前台
        }
        // 右键菜单项：退出应用
        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            // 显式释放托盘图标资源
            TrayIcon.Dispose();
            // 关闭整个应用程序
            System.Windows.Application.Current.Shutdown();
        }

        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            DependencyObject? src = e.OriginalSource as DependencyObject;
            if (WpfHelper.IsAncestorButton(src) || WpfHelper.IsAncestorScrollBar(src)) return;

            if (WpfHelper.FindAncestorBorder(src) is Border border &&
                border.Tag is WallpaperEngine.Models.WallpaperItem wallpaper)
            {
                ViewModel.HandleWallpaperSelection(
                    wallpaper,
                    (Keyboard.Modifiers & ModifierKeys.Control) != 0,
                    (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
            }
            e.Handled = true;
        }

        private void ListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            DependencyObject? src = e.OriginalSource as DependencyObject;
            if (WpfHelper.IsAncestorButton(src) || WpfHelper.IsAncestorScrollBar(src)) return;

            if (WpfHelper.FindAncestorBorder(src) is Border border &&
                border.Tag is WallpaperEngine.Models.WallpaperItem wallpaper &&
                !wallpaper.IsSelected)
            {
                ViewModel.HandleWallpaperSelection(wallpaper, false, false);
            }
            e.Handled = true;
        }

        // 防止ListBox的系统选择干扰我们的自定义选择
        private void WallpaperListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 清空ListBox的系统选择，因为我们用自定义的IsSelected属性
            if (WallpaperListBox != null)
            {
                WallpaperListBox.SelectedItem = null;
                foreach (var item in e.AddedItems)
                {
                    if (item is System.Windows.Controls.ListBoxItem listBoxItem)
                    {
                        listBoxItem.IsSelected = false;
                    }
                }
            }
        }
    }
}