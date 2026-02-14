using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;


namespace WallpaperEngine.Services {
    /// <summary>
    /// 应用程序设置服务实现，将设置以 JSON 格式存储在用户配置文件夹中
    /// </summary>
    public class SettingsService : ISettingsService {
        private readonly string _settingsFilePath;

        /// <summary>
        /// 初始化设置服务，在用户配置文件夹下创建应用目录并确定设置文件路径
        /// </summary>
        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appFolder = Path.Combine(appDataPath, "DynamicWallpaperManager");
            Directory.CreateDirectory(appFolder);
            _settingsFilePath = Path.Combine(appFolder, "settings.json");
        }

        /// <summary>
        /// 从 JSON 文件加载应用程序设置，若文件不存在或解析失败则返回默认设置
        /// </summary>
        /// <returns>应用程序设置对象</returns>
        public ApplicationSettings LoadSettings()
        {
            try {
                if (File.Exists(_settingsFilePath)) {
                    var json = File.ReadAllText(_settingsFilePath);
                    var appSettings = JsonConvert.DeserializeObject<ApplicationSettings>(json) ?? new ApplicationSettings();
                    return appSettings;
                }
            } catch (Exception ex) {
                Debug.WriteLine($"加载设置失败: {ex.Message}");
            }

            return new ApplicationSettings();
        }

        /// <summary>
        /// 将应用程序设置序列化为 JSON 并写入文件
        /// </summary>
        /// <param name="settings">要保存的设置对象</param>
        public void SaveSettings(ApplicationSettings settings)
        {
            try {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            } catch (Exception ex) {
                Debug.WriteLine($"保存设置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 验证 Wallpaper Engine 可执行文件路径是否有效
        /// </summary>
        /// <param name="path">待验证的文件路径</param>
        /// <returns>路径非空、文件存在且以 .exe 结尾时返回 true</returns>
        public bool ValidateWallpaperEnginePath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   File.Exists(path) &&
                   path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }
    }
}
