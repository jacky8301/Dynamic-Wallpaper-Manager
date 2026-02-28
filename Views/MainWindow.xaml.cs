using CommunityToolkit.Mvvm.DependencyInjection;
using Serilog;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

            // 手动添加ListBox事件处理器以避免XAML解析错误
            if (WallpaperListBox != null)
            {
                WallpaperListBox.PreviewMouseDown += ListBox_PreviewMouseDown;
                WallpaperListBox.PreviewMouseRightButtonDown += ListBox_PreviewMouseRightButtonDown;
                WallpaperListBox.SelectionChanged += WallpaperListBox_SelectionChanged;
            }
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

        // ListBox预览鼠标按下事件，处理Ctrl/Shift多选
        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            // 检查是否点击了按钮（如收藏按钮），如果是则跳过处理
            DependencyObject? originalSource = e.OriginalSource as DependencyObject;
            DependencyObject? current = originalSource;
            bool isButtonClick = false;
            while (current != null)
            {
                if (current is System.Windows.Controls.Button)
                {
                    isButtonClick = true;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            if (isButtonClick) return;

            // 检查是否点击了滚动条，如果是则跳过处理（允许滚动条正常工作）
            current = originalSource;
            bool isScrollBarClick = false;
            while (current != null)
            {
                if (current is System.Windows.Controls.Primitives.ScrollBar ||
                    current is System.Windows.Controls.Primitives.Thumb ||
                    current is System.Windows.Controls.Primitives.RepeatButton ||
                    current is System.Windows.Controls.Primitives.Track)
                {
                    isScrollBarClick = true;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            if (isScrollBarClick) return;

            // 找到被点击的Border（壁纸项）
            DependencyObject? source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is Border))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            if (source is Border border && border.Tag is WallpaperEngine.Models.WallpaperItem wallpaper)
            {
                bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                bool isShiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                ViewModel.HandleWallpaperSelection(wallpaper, isCtrlPressed, isShiftPressed);
            }

            // 始终阻止事件进一步传播，防止ListBox系统选择
            e.Handled = true;
        }

        // ListBox预览鼠标右键按下事件，更新选中项
        private void ListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;

            // 检查是否点击了按钮（如收藏按钮），如果是则跳过处理
            DependencyObject? originalSource = e.OriginalSource as DependencyObject;
            DependencyObject? current = originalSource;
            bool isButtonClick = false;
            while (current != null)
            {
                if (current is System.Windows.Controls.Button)
                {
                    isButtonClick = true;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            if (isButtonClick) return;

            // 检查是否点击了滚动条，如果是则跳过处理（允许滚动条正常工作）
            current = originalSource;
            bool isScrollBarClick = false;
            while (current != null)
            {
                if (current is System.Windows.Controls.Primitives.ScrollBar ||
                    current is System.Windows.Controls.Primitives.Thumb ||
                    current is System.Windows.Controls.Primitives.RepeatButton ||
                    current is System.Windows.Controls.Primitives.Track)
                {
                    isScrollBarClick = true;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            if (isScrollBarClick) return;

            // 找到被点击的Border（壁纸项）
            DependencyObject? source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is Border))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            if (source is Border border && border.Tag is WallpaperEngine.Models.WallpaperItem wallpaper)
            {
                // 如果右键点击的壁纸未被选中，则清空选择并选中它
                if (!wallpaper.IsSelected)
                {
                    ViewModel.HandleWallpaperSelection(wallpaper, false, false);
                }
            }

            // 始终阻止事件进一步传播，防止ListBox系统选择
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