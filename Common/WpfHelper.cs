using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WallpaperEngine.Common {
    internal static class WpfHelper {
        /// <summary>
        /// 在可视树中递归查找第一个 ScrollViewer
        /// </summary>
        internal static ScrollViewer? FindScrollViewer(DependencyObject element)
        {
            if (element is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                ScrollViewer? result = FindScrollViewer(VisualTreeHelper.GetChild(element, i));
                if (result != null) return result;
            }
            return null;
        }
    }
}
