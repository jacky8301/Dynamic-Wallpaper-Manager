using System.Windows;
using WallpaperEngine.Services;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views {
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
