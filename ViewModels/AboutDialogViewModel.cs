using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.Reflection;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 关于对话框视图模型，提供应用程序信息展示
    /// </summary>
    public partial class AboutDialogViewModel : ObservableObject {
        private const string DialogHostId = "MainRootDialog";

        public string AppName => "Dynamic Wallpaper Manager";

        public string Version {
            get {
                string? version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
                return version is not null ? $"v{version}" : "v1.0.0";
            }
        }

        public string Description => "基于 Wallpaper Engine 的壁纸管理工具，支持扫描、预览、收藏、分类和合集管理等功能。";

        public string Author => "Jacky Zheng";

        public string GitHubUrl => "https://github.com/your-repo/dynamic-wallpaper-manager";

        [RelayCommand]
        private void Close() => DialogHost.Close(DialogHostId);

        [RelayCommand]
        private void OpenGitHub()
        {
            Process.Start(new ProcessStartInfo(GitHubUrl) { UseShellExecute = true });
        }
    }
}
