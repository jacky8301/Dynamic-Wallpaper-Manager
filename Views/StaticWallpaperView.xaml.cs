using CommunityToolkit.Mvvm.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WallpaperEngine.Common;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views {
    /// <summary>
    /// 静态壁纸视图用户控件
    /// </summary>
    public partial class StaticWallpaperView : System.Windows.Controls.UserControl {
        private StaticWallpaperViewModel ViewModel;

        public StaticWallpaperView()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetService<StaticWallpaperViewModel>();
            DataContext = ViewModel;

            if (StaticWallpaperListBox != null) {
                StaticWallpaperListBox.PreviewMouseDown += ListBox_PreviewMouseDown;
                StaticWallpaperListBox.PreviewMouseRightButtonDown += ListBox_PreviewMouseRightButtonDown;
                StaticWallpaperListBox.SelectionChanged += StaticWallpaperListBox_SelectionChanged;
            }
        }

        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            DependencyObject? src = e.OriginalSource as DependencyObject;
            if (WpfHelper.IsAncestorButton(src) || WpfHelper.IsAncestorScrollBar(src)) return;

            if (WpfHelper.FindAncestorBorder(src) is Border border &&
                border.Tag is StaticWallpaperItem wallpaper) {
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
                border.Tag is StaticWallpaperItem wallpaper &&
                !wallpaper.IsSelected) {
                ViewModel.HandleWallpaperSelection(wallpaper, false, false);
            }
            e.Handled = true;
        }

        private void StaticWallpaperListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StaticWallpaperListBox != null) {
                StaticWallpaperListBox.SelectedItem = null;
            }
        }
    }
}
