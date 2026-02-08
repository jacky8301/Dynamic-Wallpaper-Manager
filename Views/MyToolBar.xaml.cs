using CommunityToolkit.Mvvm.DependencyInjection;
using System.Windows.Input;
using WallpaperEngine.ViewModels;
using TextBox = System.Windows.Controls.TextBox;

namespace WallpaperEngine.Views {
    public partial class MyToolBar : System.Windows.Controls.UserControl {
        public MyToolBar()
        {
            InitializeComponent();
            _viewModel = Ioc.Default.GetService<MainViewModel>();
            this.DataContext = _viewModel;
        }
        private MainViewModel _viewModel;
        // 保留原有的KeyDown事件处理
        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Handled) {
                return;
            }
            switch (e.Key) {
                case Key.Enter: {
                        if (DataContext is MainViewModel vm) {
                            _viewModel.SearchText = (sender as System.Windows.Controls.TextBox).Text;
                            _viewModel.SearchWallpapersCommand.Execute(null);
                        }
                    }
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }
    }
}
