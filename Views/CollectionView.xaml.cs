using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WallpaperEngine.Views {
    /// <summary>
    /// 壁纸合集视图用户控件，用于展示和管理壁纸��集
    /// </summary>
    public partial class CollectionView : System.Windows.Controls.UserControl {
        public CollectionView()
        {
            InitializeComponent();
        }

        private void CollectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                ScrollViewer scrollViewer = GetScrollViewer(WallpaperListBox);
                scrollViewer?.ScrollToHome();
            }
        }

        private static ScrollViewer GetScrollViewer(DependencyObject element)
        {
            if (element is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                ScrollViewer result = GetScrollViewer(VisualTreeHelper.GetChild(element, i));
                if (result != null) return result;
            }
            return null;
        }
    }
}
