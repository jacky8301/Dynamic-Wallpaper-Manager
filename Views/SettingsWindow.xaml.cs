using CommunityToolkit.Mvvm.DependencyInjection;
using System.Windows;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            this.DataContext = Ioc.Default.GetService<ViewModels.SettingsViewModel>();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            Ioc.Default.GetService<ViewModels.SettingsViewModel>().SaveSettings();
            Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
