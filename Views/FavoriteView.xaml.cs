using CommunityToolkit.Mvvm.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

            // 检查是否点击了滚动条
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

            e.Handled = true;
        }

        private void ListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;

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

            DependencyObject? source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is Border))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            if (source is Border border && border.Tag is WallpaperEngine.Models.WallpaperItem wallpaper)
            {
                if (!wallpaper.IsSelected)
                {
                    ViewModel.HandleWallpaperSelection(wallpaper, false, false);
                }
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
