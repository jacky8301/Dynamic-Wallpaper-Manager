// Settings.cs - 设置数据模型
using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// 应用程序设置数据模型，用于存储和管理用户配置项
/// </summary>
public partial class ApplicationSettings : ObservableObject {
    /// <summary>
    /// Wallpaper Engine 的安装路径
    /// </summary>
    [ObservableProperty]
    private string _wallpaperEnginePath;
}
