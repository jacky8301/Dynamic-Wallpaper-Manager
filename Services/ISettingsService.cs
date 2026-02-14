/// <summary>
/// 应用程序设置服务接口，提供设置的加载、保存和验证功能
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 从持久化存储中加载应用程序设置
    /// </summary>
    /// <returns>应用程序设置对象，若加载失败则返回默认设置</returns>
    ApplicationSettings LoadSettings();

    /// <summary>
    /// 将应用程序设置保存到持久化存储
    /// </summary>
    /// <param name="settings">要保存的设置对象</param>
    void SaveSettings(ApplicationSettings settings);

    /// <summary>
    /// 验证 Wallpaper Engine 可执行文件路径是否有效
    /// </summary>
    /// <param name="path">待验证的文件路径</param>
    /// <returns>路径有效返回 true，否则返回 false</returns>
    bool ValidateWallpaperEnginePath(string path);
}