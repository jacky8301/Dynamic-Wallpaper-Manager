using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using Serilog;
using WallpaperEngine.Models;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 壁纸预览服务，通过启动 Wallpaper Engine 进程实现壁纸的窗口化预览
    /// </summary>
    public class PreviewService {
        private readonly ApplicationSettings _settings;
        private readonly ISettingsService _settingsService;
        private Process? _currentPreviewProcess;
        private readonly object _previewLock = new();

        // Windows API声明
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 6;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_RESTORE = 9;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        /// <summary>
        /// 初始化预览服务并加载应用程序设置
        /// </summary>
        /// <param name="settingsService">设置服务实例</param>
        public PreviewService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.LoadSettings();
        }

        /// <summary>
        /// 预览选项配置类，定义预览窗口的标题、尺寸等参数
        /// </summary>
        public class PreviewOptions {
            public string WindowTitle { get; set; } = "Wallpaper Preview";
            public int Width { get; set; } = 800;  // 较小尺寸便于居中
            public int Height { get; set; } = 600;
            public string WindowId { get; set; } = "Wallpaper #1";
            public bool AutoClose { get; set; } = true;
            public int X { get; set; } = 0;
            public int Y { get; set; } = 0;
            public bool Activate { get; set; } = true;
        }

        /// <summary>
        /// 启动壁纸预览，会先停止已有的预览进程
        /// </summary>
        /// <param name="wallpaper">要预览的壁纸项</param>
        /// <param name="options">预览选项，为 null 时使用默认配置</param>
        /// <returns>预览进程成功启动返回 true，否则返回 false</returns>
        /// <exception cref="InvalidOperationException">Wallpaper Engine 路径未设置或不存在</exception>
        /// <exception cref="FileNotFoundException">壁纸的 project.json 文件不存在</exception>
        public bool PreviewWallpaper(WallpaperItem wallpaper, PreviewOptions? options = null)
        {
            if (wallpaper == null) return false;

            options ??= new PreviewOptions();

            Log.Information("启动壁纸预览: {FolderPath}", wallpaper.FolderPath);
            if (string.IsNullOrEmpty(_settings.WallpaperEnginePath) ||
                !File.Exists(_settings.WallpaperEnginePath)) {
                throw new InvalidOperationException("Wallpaper Engine路径未设置或不存在");
            }

            // 检查壁纸文件
            var projectJsonPath = Path.Combine(wallpaper.FolderPath, "project.json");
            if (!File.Exists(projectJsonPath)) {
                throw new FileNotFoundException($"找不到project.json文件: {projectJsonPath}");
            }

            try {
                // 停止现有的预览
                StopPreview();

                // 构建命令参数
                var processStartInfo = new ProcessStartInfo {
                    FileName = _settings.WallpaperEnginePath,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };
                PopulatePreviewArguments(processStartInfo, projectJsonPath, options);
                Log.Debug("启动Wallpaper Engine预览，参数: {Args}", string.Join(" ", processStartInfo.ArgumentList));

                lock (_previewLock) {
                    _currentPreviewProcess = Process.Start(processStartInfo);
                    bool success = _currentPreviewProcess != null && !_currentPreviewProcess.HasExited;
                    if (success) {
                        // 尝试设置预览窗口置顶
                        _ = Task.Run(async () => await TrySetPreviewWindowTopMost(_currentPreviewProcess!, options));
                    }
                    return success;
                }
            } catch (Exception ex) {
                Log.Error("启动预览失败: {Error}", ex.Message);
                throw new Exception($"启动预览失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 构建 Wallpaper Engine 预览命令行参数
        /// </summary>
        /// <param name="projectJsonPath">壁纸 project.json 文件路径</param>
        /// <param name="options">预览选项</param>
        /// <returns>拼接后的命令行参数字符串</returns>
        private static void PopulatePreviewArguments(ProcessStartInfo startInfo, string projectJsonPath, PreviewOptions options)
        {
            startInfo.ArgumentList.Add("-control");
            startInfo.ArgumentList.Add("openWallpaper");
            startInfo.ArgumentList.Add("-file");
            startInfo.ArgumentList.Add(projectJsonPath);
            startInfo.ArgumentList.Add("-playInWindow");
            startInfo.ArgumentList.Add(options.WindowId);
            startInfo.ArgumentList.Add("-width");
            startInfo.ArgumentList.Add(options.Width.ToString());
            startInfo.ArgumentList.Add("-height");
            startInfo.ArgumentList.Add(options.Height.ToString());
            startInfo.ArgumentList.Add("-paused");
            startInfo.ArgumentList.Add("false");

            if (options.X != 0) {
                startInfo.ArgumentList.Add("-x");
                startInfo.ArgumentList.Add(options.X.ToString());
            }
            if (options.Y != 0) {
                startInfo.ArgumentList.Add("-y");
                startInfo.ArgumentList.Add(options.Y.ToString());
            }
            if (options.Activate) {
                startInfo.ArgumentList.Add("-activate");
            }
        }

        private async Task TrySetPreviewWindowTopMost(Process process, PreviewOptions options)
        {
            if (process == null) return;
            // 等待窗口创建
            for (int i = 0; i < 10; i++)
            {
                if (process.HasExited) break;
                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    // 确保窗口不是最小化状态
                    ShowWindow(process.MainWindowHandle, SW_RESTORE);
                    // 设置窗口置顶
                    SetWindowPos(process.MainWindowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
                    Log.Debug("已设置预览窗口置顶");
                    break;
                }
                await Task.Delay(100);
            }
        }

        /// <summary>
        /// 停止当前正在运行的预览进程，先尝试优雅关闭，超时后强制终止
        /// </summary>
        public void StopPreview()
        {
            Process? proc;
            lock (_previewLock) {
                proc = _currentPreviewProcess;
                _currentPreviewProcess = null;
            }
            if (proc == null) return;
            try {
                Log.Debug("停止壁纸预览");
                if (!proc.HasExited) {
                    proc.CloseMainWindow();
                    if (!proc.WaitForExit(2000)) {
                        proc.Kill();
                    }
                }
            } catch (Exception ex) {
                Log.Warning("停止预览进程时出错: {Error}", ex.Message);
            } finally {
                proc.Dispose();
            }
        }

        /// <summary>
        /// 检查预览进程是否正在运行
        /// </summary>
        /// <returns>预览进程正在运行返回 true，否则返回 false</returns>
        public bool IsPreviewRunning()
        {
            lock (_previewLock) {
                try {
                    return _currentPreviewProcess != null && !_currentPreviewProcess.HasExited;
                } catch (InvalidOperationException) {
                    return false;
                }
            }
        }
    }
}