using System.Windows;
using System.Windows.Input;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WallpaperEngine.Properties;

namespace WallpaperEngine.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly ApplicationSettings _settings;
        private readonly ISettingsService _settingsService;

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.LoadSettings();

            // 初始化命令
            BrowsePathCommand = new RelayCommand(BrowsePath);
            SaveCommand = new RelayCommand(SaveSettings);
            CancelCommand = new RelayCommand(Cancel);
        }

        public string WallpaperEnginePath
        {
            get => _settings.WallpaperEnginePath;
            set
            {
                _settings.WallpaperEnginePath = value;
                OnPropertyChanged();
                ValidatePath();
            }
        }
        private string _pathStatus = "未设置";
        public string PathStatus
        {
            get => _pathStatus;
            set => SetProperty(ref _pathStatus, value);
        }

        public ICommand BrowsePathCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private void BrowsePath()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe",
                Title = "选择Wallpaper Engine工具"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                WallpaperEnginePath = openFileDialog.FileName;
            }
        }

        private void ValidatePath()
        {
            if (string.IsNullOrEmpty(WallpaperEnginePath))
            {
                PathStatus = "路径未设置";
                return;
            }

            if (!File.Exists(WallpaperEnginePath))
            {
                PathStatus = "路径不存在";
                return;
            }

            if (!WallpaperEnginePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                PathStatus = "请选择.exe文件";
                return;
            }

            PathStatus = "路径有效";
        }

        public void SaveSettings()
        {
            _settingsService.SaveSettings(_settings);
            // 关闭窗口或返回成功信号
            
        }

        private void Cancel()
        {
            // 关闭窗口
        }

        // 在SettingsViewModel中添加自动检测功能
        private void AutoDetectPath()
        {
            var possiblePaths = new[]
            {
        @"C:\Program Files (x86)\Steam\steamapps\common\wallpaper_engine\wallpaper32.exe",
        @"C:\Program Files\Steam\steamapps\common\wallpaper_engine\wallpaper32.exe",
        @"D:\Steam\steamapps\common\wallpaper_engine\wallpaper32.exe",
        // 添加其他可能的安装路径
    };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    WallpaperEnginePath = path;
                    System.Windows.MessageBox.Show("已自动检测到Wallpaper Engine路径", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            System.Windows.MessageBox.Show("无法自动检测到Wallpaper Engine路径，请手动选择", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}