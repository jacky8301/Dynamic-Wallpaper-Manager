namespace WallpaperEngine.Models {
    /// <summary>
    /// 壁纸显示方式
    /// </summary>
    public enum WallpaperFitMode {
        Fill,
        Fit,
        Stretch,
        Tile,
        Center,
        Span
    }

    /// <summary>
    /// 壁纸显示方式辅助类，提供中文显示名和注册表值映射
    /// </summary>
    public static class WallpaperFitModeHelper {
        /// <summary>
        /// 获取显示方式的中文名称
        /// </summary>
        public static string GetDisplayName(WallpaperFitMode mode) => mode switch {
            WallpaperFitMode.Fill => "填充",
            WallpaperFitMode.Fit => "适应",
            WallpaperFitMode.Stretch => "拉伸",
            WallpaperFitMode.Tile => "平铺",
            WallpaperFitMode.Center => "居中",
            WallpaperFitMode.Span => "跨区",
            _ => "填充"
        };

        /// <summary>
        /// 获取注册表 WallpaperStyle 值
        /// </summary>
        public static string GetWallpaperStyle(WallpaperFitMode mode) => mode switch {
            WallpaperFitMode.Fill => "10",
            WallpaperFitMode.Fit => "6",
            WallpaperFitMode.Stretch => "2",
            WallpaperFitMode.Tile => "0",
            WallpaperFitMode.Center => "0",
            WallpaperFitMode.Span => "22",
            _ => "10"
        };

        /// <summary>
        /// 获取注册表 TileWallpaper 值
        /// </summary>
        public static string GetTileWallpaper(WallpaperFitMode mode) => mode switch {
            WallpaperFitMode.Tile => "1",
            _ => "0"
        };
    }
}
