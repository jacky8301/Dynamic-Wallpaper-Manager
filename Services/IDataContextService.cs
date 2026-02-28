using WallpaperEngine.Models;
namespace WallpaperEngine.Services {
    /// <summary>
    /// 数据上下文服务接口，管理当前选中壁纸的共享状态
    /// </summary>
    public interface IDataContextService {
        /// <summary>
        /// 获取或设置当前选中的壁纸项
        /// </summary>
        WallpaperItem? CurrentWallpaper { get; set; }

        /// <summary>
        /// 当前选中壁纸发生变化时触发的事件
        /// </summary>
        event EventHandler<WallpaperItem?>? CurrentWallpaperChanged;
    }
}
