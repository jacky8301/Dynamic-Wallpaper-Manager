using CommunityToolkit.Mvvm.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WallpaperEngine.Common;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views {
    /// <summary>
    /// 收藏壁纸视图用户控件
    /// </summary>
    public partial class FavoriteView : System.Windows.Controls.UserControl {
        private FavoriteViewModel ViewModel;

        public FavoriteView()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetService<FavoriteViewModel>();
            DataContext = ViewModel;

            if (FavoriteWallpaperListBox != null)
            {
                FavoriteWallpaperListBox.PreviewMouseDown += ListBox_PreviewMouseDown;
                FavoriteWallpaperListBox.PreviewMouseRightButtonDown += ListBox_PreviewMouseRightButtonDown;
                FavoriteWallpaperListBox.SelectionChanged += FavoriteWallpaperListBox_SelectionChanged;
            }
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

        private void FavoriteWallpaperListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FavoriteWallpaperListBox != null)
            {
                FavoriteWallpaperListBox.SelectedItem = null;
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
