using WallpaperEngine.Models;
namespace WallpaperEngine.Services {
    public interface IDataContextService {
        WallpaperItem CurrentWallpaper { get; set; }
        event EventHandler<WallpaperItem> CurrentWallpaperChanged;
    }
}
