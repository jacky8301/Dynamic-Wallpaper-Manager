using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WallpaperEngine.Common;

namespace WallpaperEngine.Views {
    /// <summary>
    /// 壁纸合集视图用户控件，用于展示和管理壁纸合集
    /// </summary>
    public partial class CollectionView : System.Windows.Controls.UserControl {
        private ScrollViewer? _wallpaperScrollViewer;

        public CollectionView()
        {
            InitializeComponent();
        }

        private void CollectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                _wallpaperScrollViewer ??= WpfHelper.FindScrollViewer(WallpaperListBox);
                _wallpaperScrollViewer?.ScrollToHome();
            }
        }
    }
}
