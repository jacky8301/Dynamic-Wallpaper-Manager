using WallpaperEngine.Models;

namespace WallpaperEngine.Services {
    public class DataContextService : IDataContextService {
        private WallpaperItem _currentWallpaper;

        public WallpaperItem CurrentWallpaper {
            get => _currentWallpaper;
            set {
                if (_currentWallpaper != value) {
                    _currentWallpaper = value;
                    CurrentWallpaperChanged?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<WallpaperItem> CurrentWallpaperChanged;
    }
}
