using Microsoft.Win32;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using WallpaperEngine.Common;
using WallpaperEngine.Models;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 桌面壁纸设置服务，通过 Win32 API 设置 Windows 桌面壁纸
    /// </summary>
    public static class DesktopWallpaperService {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        // 支持的图片扩展名使用 FileTypeHelper.SupportedImageExtensions

        /// <summary>
        /// 设置桌面壁纸
        /// </summary>
        /// <param name="imagePath">图片文件完整路径</param>
        /// <param name="fitMode">壁纸显示方式</param>
        /// <returns>是否设置成功</returns>
        public static bool SetDesktopWallpaper(string imagePath, WallpaperFitMode fitMode = WallpaperFitMode.Fill)
        {
            if (string.IsNullOrEmpty(imagePath)) {
                Log.Warning("壁纸路径为空");
                return false;
            }

            if (!File.Exists(imagePath)) {
                Log.Warning("壁纸文件不存在: {Path}", imagePath);
                return false;
            }

            string extension = Path.GetExtension(imagePath);
            if (!FileTypeHelper.IsImageFile(extension)) {
                Log.Warning("不支持的图片格式: {Extension}", extension);
                return false;
            }

            try {
                // 设置壁纸显示方式（通过注册表）
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true)) {
                    if (key != null) {
                        key.SetValue("WallpaperStyle", WallpaperFitModeHelper.GetWallpaperStyle(fitMode));
                        key.SetValue("TileWallpaper", WallpaperFitModeHelper.GetTileWallpaper(fitMode));
                    }
                }

                int result = SystemParametersInfo(
                    SPI_SETDESKWALLPAPER,
                    0,
                    imagePath,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

                if (result != 0) {
                    Log.Information("桌面壁纸设置成功: {Path}", imagePath);
                    return true;
                } else {
                    int error = Marshal.GetLastWin32Error();
                    Log.Warning("设置桌面壁纸失败，错误代码: {ErrorCode}", error);
                    return false;
                }
            } catch (Exception ex) {
                Log.Error(ex, "设置桌面壁纸时发生异常");
                return false;
            }
        }

        /// <summary>
        /// 停止 Wallpaper Engine 动态壁纸
        /// </summary>
        /// <param name="wallpaperEnginePath">Wallpaper Engine 可执行文件路径</param>
        public static void StopWallpaperEngine(string wallpaperEnginePath)
        {
            if (string.IsNullOrEmpty(wallpaperEnginePath) || !File.Exists(wallpaperEnginePath)) {
                Log.Debug("Wallpaper Engine 路径未设置或不存在，跳过停止动态壁纸");
                return;
            }

            try {
                ProcessStartInfo startInfo = new ProcessStartInfo {
                    FileName = wallpaperEnginePath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                startInfo.ArgumentList.Add("-control");
                startInfo.ArgumentList.Add("stop");
                Process.Start(startInfo)?.Dispose();
                Log.Information("已发送停止动态壁纸命令");
            } catch (Exception ex) {
                Log.Warning(ex, "停止动态壁纸失败");
            }
        }

        /// <summary>
        /// 判断文件是否为支持的图片格式
        /// </summary>
        public static bool IsSupportedImage(string filePath)
        {
            return FileTypeHelper.IsImageFilePath(filePath);
        }
    }
}
