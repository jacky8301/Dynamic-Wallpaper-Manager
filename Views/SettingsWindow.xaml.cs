using CommunityToolkit.Mvvm.DependencyInjection;
using System.Windows;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views
{
    /// <summary>
    /// 设置窗口，提供应用程序配置选项的编辑和保存功能
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            Ioc.Default.GetService<SettingsViewModel>().SaveSettings();
            Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
