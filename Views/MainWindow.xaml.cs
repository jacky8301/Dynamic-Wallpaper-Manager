using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel(); // 然后设置 DataContext
        }
        // 双击事件
        private void WallpaperItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is WallpaperItem wallpaper)
            {
                var previewWindow = new PreviewWindow(wallpaper)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                previewWindow.ShowDialog();
            }
        }
        // MainWindow.xaml.cs 事件处理
        private void WallpaperItem_MouseEnter(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            if (border?.DataContext is WallpaperItem wallpaper)
            {
                // 延迟显示预览，避免过于敏感
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    var toolTip = border.ToolTip as ToolTip;
                    if (toolTip != null && !toolTip.IsOpen)
                    {
                        var previewControl = FindVisualChild<PreviewControl>(toolTip);
                        previewControl?.LoadPreview(wallpaper);
                        toolTip.IsOpen = true;
                    }
                };
                timer.Start();
                border.Tag = timer; // 保存计时器引用以便取消
            }
        }

        private void WallpaperItem_MouseLeave(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag is DispatcherTimer timer)
            {
                timer.Stop();
                border.Tag = null;
            }

            var toolTip = border?.ToolTip as ToolTip;
            toolTip?.SetCurrentValue(ToolTip.IsOpenProperty, false);
        }
    }
}