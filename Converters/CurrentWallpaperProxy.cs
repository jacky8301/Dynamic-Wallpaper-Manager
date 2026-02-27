using System.Windows;
using WallpaperEngine.Models;
using Serilog;

namespace WallpaperEngine.Converters
{
    /// <summary>
    /// 用于传递当前壁纸对象的代理类
    /// </summary>
    public class CurrentWallpaperProxy : Freezable
    {
        public static readonly DependencyProperty WallpaperProperty =
            DependencyProperty.Register(nameof(Wallpaper), typeof(WallpaperItem), typeof(CurrentWallpaperProxy),
                new PropertyMetadata(null, OnWallpaperChanged));

        public WallpaperItem Wallpaper
        {
            get => (WallpaperItem)GetValue(WallpaperProperty);
            set => SetValue(WallpaperProperty, value);
        }

        private static void OnWallpaperChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurrentWallpaperProxy proxy && e.NewValue is WallpaperItem wallpaper)
            {
                Log.Information($"CurrentWallpaperProxy: Wallpaper set to {wallpaper.Project?.Title}");
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new CurrentWallpaperProxy();
        }
    }
}
