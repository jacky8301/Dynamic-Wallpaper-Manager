using System.Diagnostics;
using System.Windows;
using WallpaperEngine.Models;
using WallpaperEngine.Views;
using System.Windows.Forms;

namespace WallpaperEngine.Services
{
    public class WallpaperPlayer
    {
        public void Preview(WallpaperItem wallpaper)
        {
            if (wallpaper?.Project == null) return;

            try
            {
                var previewWindow = new PreviewWindow(wallpaper);
                previewWindow.Owner = System.Windows.Application.Current.MainWindow;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"预览失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Apply(WallpaperItem wallpaper)
        {
            if (wallpaper?.Project == null) return;

            try
            {
                if (wallpaper.Project.Type == "video")
                {
                    ApplyVideoWallpaper(wallpaper.ContentPath);
                }
                else
                {
                    SetDesktopWallpaper(wallpaper.ContentPath);
                }

                System.Windows.MessageBox.Show($"壁纸 '{wallpaper.Project.Title}' 设置成功!", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置壁纸失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyVideoWallpaper(string videoPath)
        {
            // 简化实现 - 实际应用中需要更复杂的视频壁纸设置逻辑
            Process.Start("explorer.exe", videoPath);
        }

        private void SetDesktopWallpaper(string imagePath)
        {
            
        }
    }
}