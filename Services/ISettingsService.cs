// ISettingsService.cs
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

public interface ISettingsService
{
    ApplicationSettings LoadSettings();
    void SaveSettings(ApplicationSettings settings);
    bool ValidateWallpaperEnginePath(string path);
}