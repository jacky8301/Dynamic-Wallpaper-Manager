// Settings.cs - 设置数据模型
using CommunityToolkit.Mvvm.ComponentModel;

namespace WallpaperEngine.Models {
    /// <summary>
    /// 应用程序设置数据模型，用于存储和管理用户配置项
    /// </summary>
    public partial class ApplicationSettings : ObservableObject {
        /// <summary>
        /// Wallpaper Engine 的安装路径
        /// </summary>
        [ObservableProperty]
        private string _wallpaperEnginePath = string.Empty;

        /// <summary>
        /// 最后应用的壁纸类型："dynamic"（动态壁纸）或 "static"（静态壁纸）
        /// </summary>
        [ObservableProperty]
        private string _lastWallpaperType = string.Empty;

        /// <summary>
        /// 最后应用的壁纸路径：动态壁纸为 FolderPath，静态壁纸为 FilePath
        /// </summary>
        [ObservableProperty]
        private string _lastWallpaperPath = string.Empty;

        /// <summary>
        /// 静态壁纸显示方式（Fill, Fit, Stretch, Tile, Center, Span）
        /// </summary>
        [ObservableProperty]
        private string _wallpaperFitMode = "Fill";
    }
}
