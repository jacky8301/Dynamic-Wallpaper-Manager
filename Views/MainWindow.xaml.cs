using CommunityToolkit.Mvvm.DependencyInjection;
using Serilog;
using System.Windows;
using System.Windows.Input;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views {
    public partial class MainWindow : Window {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = Ioc.Default.GetService<MainViewModel>();
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
            MainViewModel vm = DataContext as MainViewModel;
            Log.Debug("LoadWallpapersAsync started");
            await vm.LoadWallpapersAsync();
            Log.Debug("LoadWallpapersAsync finish");
        }
    }
}