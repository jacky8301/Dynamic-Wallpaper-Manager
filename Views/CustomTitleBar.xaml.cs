using System.Windows;

namespace WallpaperEngine.Views {
    public partial class CustomTitleBar : System.Windows.Controls.UserControl {
        // 定义依赖属性，用于绑定标题文本
        public string Title {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(CustomTitleBar), new PropertyMetadata("App Title"));

        public CustomTitleBar()
        {
            InitializeComponent();
        }

        // 查找父窗口并执行操作
        private Window FindParentWindow()
        {
            return Window.GetWindow(this);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = FindParentWindow();
            if (window != null)
                window.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = FindParentWindow();
            if (window != null) {
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = FindParentWindow();
            window?.Close();
        }
    }
}

