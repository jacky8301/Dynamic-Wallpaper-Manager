using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WallpaperEngine.Views
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 更新标题栏显示的图标
        /// </summary>
        private void UpdateTitleBarIcon()
        {
            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    // 使用Image控件加载图片文件
                    TitleBarIcon.Source = new BitmapImage(new Uri(iconPath));
                }
            }
            catch (Exception ex)
            {
                // 图标加载失败不影响主要功能，但可记录日志
                System.Diagnostics.Debug.WriteLine("标题栏图标加载失败: " + ex.Message);
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.MainViewModel();
            UpdateTitleBarIcon();
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
                    // 可选：调整窗口位置，避免鼠标位置和窗口偏移过大
                }
                DragMove();
            }
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // 最大化时设置边距，防止内容溢出屏幕
                BorderThickness = new Thickness(8);
            }
            else
            {
                BorderThickness = new Thickness(0);
            }
        }
    }
}