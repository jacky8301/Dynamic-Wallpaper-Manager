using WallpaperEngine.Models;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 数据上下文服务实现，用于在不同视图之间共享当前选中壁纸的状态
    /// </summary>
    public class DataContextService : IDataContextService {
        private WallpaperItem _currentWallpaper;

        /// <summary>
        /// 获取或设置当前选中的壁纸项，值变化时触发 <see cref="CurrentWallpaperChanged"/> 事件
        /// </summary>
        public WallpaperItem CurrentWallpaper {
            get => _currentWallpaper;
            set {
                if (_currentWallpaper != value) {
                    _currentWallpaper = value;
                    CurrentWallpaperChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// 当前选中壁纸发生变化时触发的事件
        /// </summary>
        public event EventHandler<WallpaperItem> CurrentWallpaperChanged;
    }
}
