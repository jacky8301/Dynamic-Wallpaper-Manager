using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows.Input;

namespace WallpaperEngine.ViewModels {
    public class SettingsViewModel : ObservableObject {
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

        public string WallpaperEnginePath {
            get => _settings.WallpaperEnginePath;
            set {
                _settings.WallpaperEnginePath = value;
                OnPropertyChanged();
                ValidatePath();
            }
        }
        private string _pathStatus = "未设置";
        public string PathStatus {
            get => _pathStatus;
            set => SetProperty(ref _pathStatus, value);
        }

        public ICommand BrowsePathCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

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

        public void SaveSettings()
        {
            _settingsService.SaveSettings(_settings);
        }

        private void Cancel()
        {
        }
    }
}