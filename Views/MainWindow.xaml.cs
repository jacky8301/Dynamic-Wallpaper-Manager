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

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Handled) {
                return;
            }
            switch (e.Key) {
                case Key.Enter: {
                        if (DataContext is MainViewModel vm) {
                            vm.SearchText = (sender as System.Windows.Controls.TextBox).Text;
                            vm.SearchWallpapersCommand.Execute(null);
                        }
                    }
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void OnClickPreviewButton(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm) {
                vm.PreviewWallpaperCommand.Execute(vm.SelectedWallpaper);
            }
        }

        private void OnClickDeleteWallpaperButton(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm) {
                vm.DeleteWallpaperCommand.Execute(vm.SelectedWallpaper);
            }
        }

        private void OnClickApplyWallpaperButton(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm) {
                vm.ApplyWallpaperCommand.Execute(vm.SelectedWallpaper);
            }
        }

        private void OnClickFavoriteButton(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm) {
                vm.ToggleFavoriteCommand.Execute(vm.SelectedWallpaper);
            }
        }
    }
}