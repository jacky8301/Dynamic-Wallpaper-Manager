using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;
using WpfScrollBar = System.Windows.Controls.Primitives.ScrollBar;

namespace WallpaperEngine.Common {
    internal static class WpfHelper {
        /// <summary>
        /// 在可视树中递归查找第一个 ScrollViewer（向下搜索子节点）
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

        /// <summary>
        /// 沿可视树向上查找，判断点击源是否在 Button 内部
        /// </summary>
        internal static bool IsAncestorButton(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is WpfButton) return true;
                source = VisualTreeHelper.GetParent(source);
            }
            return false;
        }

        /// <summary>
        /// 沿可视树向上查找，判断点击源是否在滚动条相关控件内部
        /// </summary>
        internal static bool IsAncestorScrollBar(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is WpfScrollBar or Thumb or RepeatButton or Track) return true;
                source = VisualTreeHelper.GetParent(source);
            }
            return false;
        }

        /// <summary>
        /// 沿可视树向上查找第一个 Border（壁纸项容器）
        /// </summary>
        internal static Border? FindAncestorBorder(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is Border border) return border;
                source = VisualTreeHelper.GetParent(source);
            }
            return null;
        }
    }
}
