using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.IO;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 设置视图模型，管理Wallpaper Engine路径配置和验证
    /// </summary>
    public partial class SettingsViewModel : ObservableObject {
        private readonly ApplicationSettings _settings;
        private readonly ISettingsService _settingsService;

        /// <summary>路径验证状态描述文本</summary>
        [ObservableProperty]
        private string _pathStatus = "未设置";

        /// <summary>
        /// 初始化设置视图模型，加载设置并验证路径
        /// </summary>
        /// <param name="settingsService">设置服务</param>
        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.LoadSettings();
            ValidatePath();
        }

        /// <summary>
        /// Wallpaper Engine可执行文件路径，设置时自动验证路径有效性
        /// </summary>
        public string WallpaperEnginePath {
            get => _settings.WallpaperEnginePath;
            set {
                if (SetProperty(_settings.WallpaperEnginePath, value, _settings, (s, v) => s.WallpaperEnginePath = v)) {
                    ValidatePath();
                }
            }
        }

        /// <summary>
        /// 浏览路径命令，打开文件选择对话框选择Wallpaper Engine可执行文件
        /// </summary>
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

        /// <summary>
        /// 保存当前设置到持久化存储
        /// </summary>
        public void SaveSettings()
        {
            Log.Information("保存应用设置");
            _settingsService.SaveSettings(_settings);
        }

        /// <summary>
        /// 验证Wallpaper Engine路径的有效性，更新PathStatus状态
        /// </summary>
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
            Log.Debug("Wallpaper Engine 路径验证通过: {Path}", WallpaperEnginePath);
        }
    }
}
