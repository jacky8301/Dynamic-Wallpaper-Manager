public interface ISettingsService
{
    ApplicationSettings LoadSettings();
    void SaveSettings(ApplicationSettings settings);
    bool ValidateWallpaperEnginePath(string path);
}