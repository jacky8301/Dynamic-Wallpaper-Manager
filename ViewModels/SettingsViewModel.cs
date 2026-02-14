using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace WallpaperEngine.ViewModels {
    public partial class SettingsViewModel : ObservableObject {
        private readonly ApplicationSettings _settings;
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        private string _pathStatus = "未设置";

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.LoadSettings();
            ValidatePath();
        }

        public string WallpaperEnginePath {
            get => _settings.WallpaperEnginePath;
            set {
                if (SetProperty(_settings.WallpaperEnginePath, value, _settings, (s, v) => s.WallpaperEnginePath = v)) {
                    ValidatePath();
                }
            }
        }

        [RelayCommand]
        private void BrowsePath()
        {
            var openFileDialog = new OpenFileDialog {
                Filter = "可执行文件 (*.exe)|*.exe",
                Title = "选择Wallpaper Engine工具"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                WallpaperEnginePath = openFileDialog.FileName;
            }
        }

        public void SaveSettings()
        {
            _settingsService.SaveSettings(_settings);
        }

        private void ValidatePath()
        {
            if (string.IsNullOrEmpty(WallpaperEnginePath)) {
                PathStatus = "路径未设置";
                return;
            }
            if (!File.Exists(WallpaperEnginePath)) {
                PathStatus = "路径不存在";
                return;
            }
            if (!WallpaperEnginePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) {
                PathStatus = "请选择.exe文件";
                return;
            }
            PathStatus = "路径有效";
        }
    }
}
