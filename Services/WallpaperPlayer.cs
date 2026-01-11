// WallpaperPlayer.cs
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WallpaperEngine.Models;
using WallpaperEngine.Views;

namespace WallpaperEngine.Services
{
    public class WallpaperPlayer
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        public void Preview(WallpaperItem wallpaper)
        {
            // 创建预览窗口
            var previewWindow = new PreviewWindow(wallpaper);
            previewWindow.ShowDialog();
        }

        public void Apply(WallpaperItem wallpaper)
        {
            try
            {
                if (wallpaper.Project.Type == "video")
                {
                    ApplyVideoWallpaper(wallpaper.ContentPath);
                }
                // 其他类型的壁纸处理...
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"应用壁纸时出错: {ex.Message}");
            }
        }

        private void ApplyVideoWallpaper(string videoPath)
        {
            // 使用Windows API将视频窗口设置为壁纸
            // 这里需要复杂的Windows API调用，简化实现
            Process.Start("explorer.exe", videoPath);
        }
    }
}