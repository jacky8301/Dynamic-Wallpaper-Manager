using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;


namespace WallpaperEngine.Services {
    public class SettingsService : ISettingsService {
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appFolder = Path.Combine(appDataPath, "DynamicWallpaperManager");
            Directory.CreateDirectory(appFolder);
            _settingsFilePath = Path.Combine(appFolder, "settings.json");
        }

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

        public bool ValidateWallpaperEnginePath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   File.Exists(path) &&
                   path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }
    }
}
