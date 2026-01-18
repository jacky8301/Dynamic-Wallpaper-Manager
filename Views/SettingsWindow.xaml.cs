using System.Windows;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.SettingsViewModel(new Services.SettingsService());
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.SettingsViewModel? vm = this.DataContext as SettingsViewModel;
            vm?.SaveSettings();
            Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
