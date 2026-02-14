using CommunityToolkit.Mvvm.DependencyInjection;
using Serilog;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views {
    public partial class MainWindow : System.Windows.Window {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetService<MainViewModel>();
            this.DataContext = ViewModel;
            ViewModel.LoadWallpapersCompleted += (s, e) => {
                // 在壁纸加载完成后隐藏加载层
                Dispatcher.Invoke(() => HideLoadingOverlay());
            };           
        }
        private MainViewModel ViewModel;
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
    }
}