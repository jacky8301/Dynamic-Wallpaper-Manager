using CommunityToolkit.Mvvm.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using WallpaperEngine.ViewModels;
using TextBox = System.Windows.Controls.TextBox;

namespace WallpaperEngine.Views {
    public partial class MyToolBar : System.Windows.Controls.UserControl {
        private MainViewModel viewModel => Ioc.Default.GetService<MainViewModel>();
        public MyToolBar()
        {
            InitializeComponent();
        }
        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Handled) {
                return;
            }
            switch (e.Key) {
                case Key.Enter: {                        
                        viewModel.SearchText = SearchTextBox.Text;
                        viewModel.SearchWallpapersCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            viewModel.SearchText = string.Empty;
            viewModel.SearchWallpapersCommand.Execute(null);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SearchText = SearchTextBox.Text;
            viewModel.SearchWallpapersCommand.Execute(null);
        }
    }
}
