using System.Windows;
using WallpaperEngine.Services;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views {
    /// <summary>
    /// Material Design 风格的对话框窗口，用于显示确认提示等交互内容
    /// </summary>
    public partial class MaterialDialogWindow : Window {
        public MaterialDialogWindow(MaterialDialogParams parameters)
        {
            InitializeComponent();
            DataContext = new ConfirmationDialogViewModel(parameters);

            // 设置窗口样式
            Style = (Style)FindResource("MaterialDesignWindow");
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
        }
    }
}
